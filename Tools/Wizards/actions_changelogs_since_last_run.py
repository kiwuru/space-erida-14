#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import os
import re
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable

import requests
import yaml

DEBUG = False
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")

# https://discord.com/developers/docs/resources/webhook
DISCORD_EMBED_DESCRIPTION_LIMIT = 3900
MAX_CHANGE_MESSAGE_LENGTH = 1000
DISCORD_MESSAGE_DELAY_SECONDS = 0.5
ERIDA_EMBED_COLOR = 0x6D3286

CHANGELOG_FILE = "Resources/Changelog/Erida.yml" # Erida edit

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}
TYPES_TO_LABELS = {
    "Fix": "Исправлено",
    "Add": "Добавлено",
    "Remove": "Удалено",
    "Tweak": "Изменено",
}

ChangelogEntry = dict[str, Any]


def main():
    webhook_erida = os.environ.get("DISCORD_WEBHOOK_URL_ERIDA")

    if not webhook_erida:
        raise RuntimeError("DISCORD_WEBHOOK_URL_ERIDA is not set")

    if DEBUG:
        # to debug this script locally, you can use
        # a separate local file as the old changelog
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text()
        publish_context = None
    else:
        # when running this normally in a GitHub actions workflow,
        # it will get the old changelog from the GitHub API
        last_changelog_stream, publish_context = get_last_changelog()

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f)

    diff = list(diff_changelog(last_changelog, cur_changelog))
    entries = list(group_changelog_entries_by_pull_request(diff))

    if not entries:
        print("No new changelog entries since the last successful publish")
        return

    change_count = sum(len(entry.get("changes") or []) for entry in entries)
    print(
        f"Sending {len(entries)} pull request changelog entries "
        f"with {change_count} changes since the last successful publish"
    )
    send_changelog(changelog_entries_to_embeds(entries), entries, publish_context)


def get_most_recent_workflow(
    sess: requests.Session, current_run: dict[str, Any]
) -> Any:
    past_runs = get_past_runs(sess, current_run)
    for run in past_runs["workflow_runs"]:
        # First past successful run that isn't our current run.
        if run["id"] == current_run["id"]:
            continue

        return run

    raise RuntimeError("No previous successful publish workflow run found")


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}"
    )
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    """
    Get all successful workflow runs before our current one.
    """
    params = {"status": "success", "created": f"<={current_run['created_at']}"}
    resp = sess.get(f"{current_run['workflow_url']}/runs", params=params)
    resp.raise_for_status()
    return resp.json()


def get_last_changelog() -> tuple[str, dict[str, Any]]:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    current_run = get_current_run(session, github_repository, github_run)
    most_recent = get_most_recent_workflow(session, current_run)
    last_sha = most_recent["head_sha"]
    print(f"Last successful publish job was {most_recent['id']}: {last_sha}")
    last_changelog_stream = get_last_changelog_by_sha(
        session, last_sha, github_repository
    )

    return last_changelog_stream, {
        "github_repository": github_repository,
        "current_run": current_run,
        "previous_run": most_recent,
    }


def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    """
    Use GitHub API to get the previous version of the changelog YAML (Actions builds are fetched with a shallow clone)
    """
    params = {
        "ref": sha,
    }
    headers = {"Accept": "application/vnd.github.raw"}

    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers=headers,
        params=params,
    )
    resp.raise_for_status()
    return resp.text


def diff_changelog(
    old: dict[str, Any], cur: dict[str, Any]
) -> Iterable[ChangelogEntry]:
    """
    Find all new entries not present in the previous publish.
    """
    old_entry_keys = {get_entry_pull_request_key(e) for e in old.get("Entries", [])}
    return (
        e
        for e in cur.get("Entries", [])
        if get_entry_pull_request_key(e) not in old_entry_keys
    )


def group_changelog_entries_by_pull_request(
    entries: Iterable[ChangelogEntry],
) -> Iterable[ChangelogEntry]:
    """
    Discord messages are grouped by PR so players can react to every PR separately.
    """
    grouped: dict[str, ChangelogEntry] = {}

    for entry in entries:
        changes = list(entry.get("changes") or [])
        if not changes:
            print(f"Skipping changelog entry {entry.get('id')} without changes")
            continue

        key = get_entry_pull_request_key(entry)
        existing = grouped.get(key)
        if existing:
            existing.setdefault("changes", []).extend(changes)
            continue

        grouped[key] = dict(entry, changes=changes)

    return grouped.values()


def get_entry_pull_request_key(entry: ChangelogEntry) -> str:
    url = str(entry.get("url") or "").strip()
    if url:
        return url

    return f"{entry.get('id')}:{entry.get('title')}"


def get_discord_body(embed: dict[str, Any]) -> dict[str, Any]:
    return {
        "embeds": [embed],
        "allowed_mentions": {"parse": []},
        "flags": 0,
    }


def send_with_retry(webhook_url: str, body: dict[str, Any], name: str) -> None:
    retry_attempt = 0
    max_retries = 30

    while True:
        try:
            response = requests.post(webhook_url, json=body, timeout=20)
            if response.status_code in (429, 500, 502, 503, 504):
                retry_attempt += 1
                if retry_attempt > max_retries:
                    raise RuntimeError(f"[{name}] Too many retries, giving up")
                retry_after = get_retry_after(response, retry_attempt)
                print(
                    f"[{name}] Discord returned {response.status_code}, "
                    f"retrying after {retry_after} seconds"
                )
                time.sleep(retry_after)
                continue

            response.raise_for_status()
            print(f"Sent to {name} webhook")
            break
        except requests.exceptions.RequestException as e:
            retry_attempt += 1
            if retry_attempt > max_retries:
                raise RuntimeError(f"[{name}] Failed to send message") from e

            retry_after = min(30, 2 ** min(retry_attempt, 5))
            print(f"[{name}] Request failed, retrying after {retry_after} seconds")
            time.sleep(retry_after)


def get_retry_after(response: requests.Response, retry_attempt: int) -> float:
    try:
        retry_after = float(response.json().get("retry_after", 0))
    except (AttributeError, ValueError, TypeError):
        retry_after = 0

    if retry_after <= 0:
        retry_after_header = response.headers.get("Retry-After")
        if retry_after_header:
            try:
                retry_after = float(retry_after_header)
            except ValueError:
                retry_after = 0

    if retry_after <= 0:
        retry_after = min(30, 2 ** min(retry_attempt, 5))

    return retry_after + 0.25


def send_discord_webhook(embed: dict[str, Any], message_name: str) -> None:
    webhook_url_erida = os.environ.get("DISCORD_WEBHOOK_URL_ERIDA")

    if webhook_url_erida:
        send_with_retry(webhook_url_erida, get_discord_body(embed), message_name)

    if not webhook_url_erida:
        raise RuntimeError("No Discord webhooks configured!")


def changelog_entries_to_embeds(entries: Iterable[ChangelogEntry]) -> list[dict[str, Any]]:
    """Process structured changelog entries into Discord embeds."""
    return [entry_to_embed(entry) for entry in entries]


def entry_to_embed(entry: ChangelogEntry) -> dict[str, Any]:
    lines: list[str] = []
    grouped_changes: dict[str, list[str]] = {}

    for change in entry.get("changes") or []:
        change_type = str(change.get("type") or "Other")
        emoji = TYPES_TO_EMOJI.get(change_type, "❓")
        message = truncate(str(change.get("message") or ""), MAX_CHANGE_MESSAGE_LENGTH)
        if not message:
            continue

        grouped_changes.setdefault(change_type, []).append(f"- {emoji} {message}")

    for change_type, changes in grouped_changes.items():
        if lines:
            lines.append("")
        label = TYPES_TO_LABELS.get(change_type, "Прочее")
        lines.append(f"**{label}**")
        lines.extend(changes)

    description = truncate(
        "\n".join(lines) or "Без описанных изменений",
        DISCORD_EMBED_DESCRIPTION_LIMIT,
    )
    title = format_entry_title(entry)

    embed: dict[str, Any] = {
        "title": title,
        "description": description,
        "color": ERIDA_EMBED_COLOR,
        "footer": {
            "text": truncate(str(entry.get("author") or "Unknown"), 256),
        },
    }

    url = entry.get("url")
    if url and str(url).strip():
        embed["url"] = str(url)

    avatar_url = entry.get("avatar_url")
    if avatar_url and str(avatar_url).strip():
        embed["footer"]["icon_url"] = str(avatar_url)

    timestamp = entry.get("time")
    if timestamp:
        embed["timestamp"] = str(timestamp)

    return embed


def format_entry_title(entry: ChangelogEntry) -> str:
    title = str(entry.get("title") or "Changelog update")
    pr_number = get_pull_request_number(entry.get("url"))
    if pr_number:
        title = f"#{pr_number} - {title}"

    return truncate(title, 256)


def get_pull_request_number(url: Any) -> str | None:
    if not url:
        return None

    match = re.search(r"/pull/(\d+)", str(url))
    if not match:
        return None

    return match.group(1)


def create_header_embed(
    entries: list[ChangelogEntry], publish_context: dict[str, Any] | None
) -> dict[str, Any]:
    change_count = sum(len(entry.get("changes") or []) for entry in entries)
    current_run = (publish_context or {}).get("current_run") or {}
    previous_run = (publish_context or {}).get("previous_run") or {}
    github_repository = (publish_context or {}).get("github_repository")

    previous_update = format_discord_date(
        previous_run.get("updated_at") or previous_run.get("created_at")
    )
    version = format_version_link(github_repository, current_run.get("head_sha"))

    description = (
        f"Предыдущее обновление: {previous_update}\n"
        f"Изменений: **{format_plural(len(entries), 'PR', 'PR', 'PR')}**, "
        f"**{format_plural(change_count, 'пункт', 'пункта', 'пунктов')}**\n"
        f"Версия: {version}"
    )

    embed: dict[str, Any] = {
        "title": "Эрида - обновление серверов",
        "description": description,
        "color": ERIDA_EMBED_COLOR,
    }

    if github_repository and current_run.get("head_sha"):
        embed["url"] = get_commit_tree_url(github_repository, current_run["head_sha"])

    current_timestamp = current_run.get("updated_at") or current_run.get("created_at")
    if current_timestamp:
        embed["timestamp"] = str(current_timestamp)

    return embed


def format_discord_date(value: Any) -> str:
    if not value:
        return "неизвестно"

    try:
        timestamp = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    except ValueError:
        return str(value)

    if timestamp.tzinfo is None:
        timestamp = timestamp.replace(tzinfo=timezone.utc)

    return f"<t:{int(timestamp.timestamp())}:d>"


def format_version_link(github_repository: Any, sha: Any) -> str:
    if not sha:
        return "неизвестно"

    short_sha = str(sha)[:7]
    if not github_repository:
        return short_sha

    return f"[{short_sha}]({get_commit_tree_url(github_repository, sha)})"


def format_plural(value: int, one: str, few: str, many: str) -> str:
    remainder = value % 100
    if 11 <= remainder <= 14:
        word = many
    else:
        last_digit = value % 10
        if last_digit == 1:
            word = one
        elif 2 <= last_digit <= 4:
            word = few
        else:
            word = many

    return f"{value} {word}"


def get_commit_tree_url(github_repository: Any, sha: Any) -> str:
    return f"https://github.com/{github_repository}/tree/{sha}"


def truncate(value: str, limit: int) -> str:
    if len(value) <= limit:
        return value

    return value[: limit - 6].rstrip() + " [...]"


def send_changelog(
    embeds: list[dict[str, Any]],
    entries: list[ChangelogEntry],
    publish_context: dict[str, Any] | None,
) -> None:
    header_embed = create_header_embed(entries, publish_context)
    send_discord_webhook(header_embed, "Erida changelog header")
    time.sleep(DISCORD_MESSAGE_DELAY_SECONDS)

    for index, embed in enumerate(embeds, start=1):
        send_discord_webhook(embed, f"Erida changelog PR {index}/{len(embeds)}")
        if index != len(embeds):
            time.sleep(DISCORD_MESSAGE_DELAY_SECONDS)


if __name__ == "__main__":
    main()

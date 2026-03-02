"""Publish tool — snapshot Crest package subtree onto the ``package`` branch and push."""

from __future__ import annotations

import subprocess
import sys
from typing import Any

import click

from repo_tools.core import RepoTool, ToolContext, logger


def _git(*args: str, cwd: str | None = None) -> str:
    result = subprocess.run(
        ["git", *args],
        capture_output=True, text=True, cwd=cwd,
    )
    if result.returncode != 0:
        logger.error(f"git {' '.join(args)} failed:\n{result.stderr.strip()}")
        sys.exit(1)
    return result.stdout.strip()


class PublishTool(RepoTool):
    name = "publish"
    help = "Snapshot Crest package subtree onto the package branch and push"

    def setup(self, cmd: click.Command) -> click.Command:
        cmd = click.option("--dry-run", is_flag=True, help="Print what would happen without executing")(cmd)
        return cmd

    def execute(self, ctx: ToolContext, args: dict[str, Any]) -> None:
        dry_run = args.get("dry_run", False)
        root = str(ctx.workspace_root)
        subtree = "crest/Assets/Crest"

        tree = _git("rev-parse", f"HEAD:{subtree}", cwd=root)
        short_hash = _git("rev-parse", "--short", "HEAD", cwd=root)
        subject = _git("log", "-1", "--format=%s", cwd=root)
        branch = _git("rev-parse", "--abbrev-ref", "HEAD", cwd=root)
        msg = f"Publish from {short_hash} - {subject}"

        if dry_run:
            logger.info(f"Would create commit-tree {tree[:12]} -p package -m {msg!r}")
            logger.info(f"Would update-ref refs/heads/package to new commit")
            logger.info(f"Would push {branch} and package")
            return

        logger.info(f"Creating package commit: {msg}")
        commit = _git("commit-tree", tree, "-p", "package", "-m", msg, cwd=root)
        _git("update-ref", "refs/heads/package", commit, cwd=root)
        logger.info(f"Pushing {branch} and package...")
        _git("push", "origin", branch, "package", cwd=root)
        logger.info("Published successfully")

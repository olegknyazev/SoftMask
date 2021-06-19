#!/bin/sh

# It does "perceptually compare" all the changed .pngs in modified git status output.
# A handly tool when migrating to newer Unity version.

SCRIPT_DIR="$(dirname "$0")"
export GIT_EXTERNAL_DIFF="$SCRIPT_DIR/imagediff.sh"
git status -s | egrep -e '\s?M.+\.png$' | awk '{print $2}' | xargs git --no-pager diff

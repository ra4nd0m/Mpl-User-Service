#!/bin/bash

set -euo pipefail
export PYTHONUNBUFFERED=1 
exec /usr/bin/systemd-cat -t analytics-spreadsheet-parser -p info \
    /opt/parsers/externally-scheduled/analytics-spreadsheet-parser/venv/bin/python3 \
    /opt/parsers/externally-scheduled/analytics-spreadsheet-parser/src/table_parser.py
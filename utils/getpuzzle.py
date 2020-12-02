#!/usr/bin/env python3
# Grab a puzzle from lichess.org.
import sys
import json
import requests

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print('EXAMPLE: {} https://lichess.org/training/62383'.format(sys.argv[0]))
        sys.exit(1)

    url = sys.argv[1]
    req = requests.get(url = url)

    if req.status_code > 299:
        print('FAIL: status code {}'.format(req.status_code))

    prefix = '{LichessPuzzle('
    suffix = ')})</script></body></html>'

    text = req.text
    frontIndex = text.find(prefix)
    backIndex = text.rfind(suffix)
    puzzleIndex = frontIndex + len(prefix)
    if frontIndex < 0 or backIndex < puzzleIndex:
        print('ERROR: Cannot find puzzle JSON in response')
        sys.exit(1)

    puzzle = json.loads(text[puzzleIndex : backIndex])
    print(json.dumps(puzzle, indent=2))
    sys.exit(0)

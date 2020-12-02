#!/usr/bin/env python3
# Grab a puzzle from lichess.org via web-scraping.
import sys
import re
import json
import requests

def PrintWinningLines(node, path):
    if node == 'win':
        print(path.strip())
    else:
        for (move, child) in node.items():
            PrintWinningLines(child, path + ' ' + move)

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print('EXAMPLE: {} 62379'.format(sys.argv[0]))
        sys.exit(1)

    url = sys.argv[1]
    if re.match(r'^[0-9]+$', url):
        url = 'https://lichess.org/training/' + url

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

    data = json.loads(text[puzzleIndex : backIndex])['data']
    #print(json.dumps(data, indent=2))
    game = data['game']['treeParts']
    puzzle = data['puzzle']
    # Find the initial FEN by searching the game for the "initial ply" node.
    initialPly = puzzle['initialPly']
    fen = None
    for node in game:
        if node['ply'] == initialPly:
            fen = node['fen']
            break
    if fen is None:
        print('ERROR: could not find initial FEN.')
        sys.exit(1)
    PrintWinningLines(puzzle['lines'], '')
    print(fen)
    print(url)
    sys.exit(0)

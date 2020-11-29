#!/usr/bin/env python3
import sys
import re

r"""
Example PGN input:

[Event "Rated Rapid game"]
[Site "https://lichess.org/MgNy9lzj"]
[Date "2020.10.01"]
[Round "-"]
[White "Cost2Be3"]
[Black "mariocbsf"]
[Result "1-0"]
[UTCDate "2020.10.01"]
[UTCTime "00:00:00"]
[WhiteElo "1254"]
[BlackElo "1262"]
[WhiteRatingDiff "+6"]
[BlackRatingDiff "-6"]
[ECO "C23"]
[Opening "Bishop's Opening: Philidor Counterattack"]
[TimeControl "600+5"]
[Termination "Normal"]

1. e4 { [%clk 0:10:00] } e5 { [%clk 0:10:00] } 2. Bc4 { [%clk 0:09:59] } c6 { [%clk 0:09:57] } 3. Qf3 { [%clk 0:10:01] } b5 { [%clk 0:10:00] } 4. Qxf7# { [%clk 0:09:57] } 1-0
"""

MIN_ELO = 2400
MIN_TIME_CONTROL = 600

reIntegerFront = re.compile(r'^[0-9]+')

def KeepGame(header):
    whiteElo = int(header['WhiteElo'])
    blackElo = int(header['BlackElo'])
    if (whiteElo < MIN_ELO) or (blackElo < MIN_ELO):
        return False
    tc = reIntegerFront.match(header['TimeControl'])
    if (not tc) or int(tc.group(0)) < MIN_TIME_CONTROL:
        return False
    return header['Termination'] == 'Normal'

if __name__ == '__main__':
    inFileName = 'lichess_db_standard_rated_2020-10.pgn'
    outFileName = 'filter.pgn'
    totalGameCount = 0
    keptGameCount = 0
    with open(inFileName, 'rt') as infile:
        with open(outFileName, 'wt') as outfile:
            reTag = re.compile(r'^\s*\[\s*([A-Za-z0-9_]+)\s*"((\\[\\"]|[^"])*)"\s*\]\s*$')
            lnum = 0
            state = 0
            listing = ''
            header = {}
            for line in infile:
                lnum += 1
                listing += line
                line = line.strip()
                if line == '':
                    state += 1
                    if state == 2:
                        # We have a complete game. Decide whether to emit it or not.
                        totalGameCount += 1
                        if KeepGame(header):
                            outfile.write(listing)
                            keptGameCount += 1
                        state = 0
                        listing = ''
                        header = {}
                elif state == 0:    # header
                    m = reTag.match(line)
                    if m:
                        key = m.group(1)
                        value = m.group(2)  # assume none of the values we care about contains backslashes
                        header[key] = value
                        # print('{0} = "{1}"'.format(key, value))
    print('Read {0} games, kept {1}.'.format(totalGameCount, keptGameCount))
    sys.exit(0)

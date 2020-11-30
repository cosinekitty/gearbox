#!/usr/bin/env python3
# Generate hash salt for Gearbox chess engine.
import sys

def NextHashSalt(rand):
    return '0x' + ''.join('{:02x}'.format(b) for b in rand.read(8)) + 'UL'

def NextPair(rand):
    return NextHashSalt(rand), NextHashSalt(rand)

def CastlingFlagComment(index, flag, text):
    if index & flag:
        return ' ' + text
    return ' --'

if __name__ == '__main__':
    with open('/dev/urandom', 'rb') as rand:
        with open('../src/Gearbox/HashSalt.cs', 'wt') as code:
            code.write('namespace Gearbox\n')
            code.write('{\n')
            code.write('    internal static class HashSalt\n')
            code.write('    {\n')

            code.write('        internal static readonly ulong[,] Castling = new ulong[16, 2]\n')
            code.write('        {\n')
            for i in range(16):
                a, b = NextPair(rand)
                comment  = CastlingFlagComment(i, 8, 'BQ')
                comment += CastlingFlagComment(i, 4, 'BK')
                comment += CastlingFlagComment(i, 2, 'WQ')
                comment += CastlingFlagComment(i, 1, 'WK')
                code.write('            { ' + a + ', ' + b + ' },  //' + comment + '\n')
            code.write('        };\n\n')

            code.write('        internal static readonly ulong[,,] Data = new ulong[12, 64, 2]\n')
            code.write('        {\n')
            for piece in 'PNBRQKpnbrqk':
                code.write('            {\n')
                for i in range(64):
                    f = 'abcdefgh'[i & 7]
                    r = '12345678'[i >> 3]
                    a, b = NextPair(rand)
                    if piece + r == 'P1':
                        if f == 'a':
                            comment = 'White to move'
                        else:
                            comment = '(not used)'
                    elif piece + r == 'P8':
                        comment = 'en passant target on ' + f + '-file'
                    elif (piece + r) in ['p1', 'p8']:
                        comment = '(not used)'
                    else:
                        comment = piece + ' ' + f + r
                    comment = '[{:2d}] {}'.format(i, comment)
                    code.write('                { ' + a + ', ' + b + ' },  // ' + comment + '\n')
                code.write('            },\n')
            code.write('        };\n')

            code.write('    }\n')
            code.write('}\n')
    sys.exit(0)

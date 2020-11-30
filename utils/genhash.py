#!/usr/bin/env python3
# Generate hash salt for Gearbox chess engine.
import sys

def NextHashSalt(rand):
    return '0x' + ''.join('{:02x}'.format(b) for b in rand.read(8)) + 'UL'

if __name__ == '__main__':
    with open('/dev/urandom', 'rb') as rand:
        with open('../src/Gearbox/HashSalt.cs', 'wt') as code:
            code.write('namespace Gearbox\n')
            code.write('{\n')
            code.write('    internal static class HashSalt\n')
            code.write('    {\n')

            a = NextHashSalt(rand)
            b = NextHashSalt(rand)
            code.write('        internal static readonly ulong[,,] Data = new ulong[12, 64, 2]\n')
            code.write('        {\n')
            for piece in 'PNBRQKpnbrqk':
                code.write('            {\n')
                for i in range(64):
                    f = 'abcdefgh'[i & 7]
                    r = '12345678'[i >> 3]
                    a = NextHashSalt(rand)
                    b = NextHashSalt(rand)
                    if piece + r == 'P1':
                        if f == 'a':
                            comment = 'White to move'
                        elif f == 'b':
                            comment = 'White can O-O'
                        elif f == 'c':
                            comment = 'White can O-O-O'
                        elif f == 'd':
                            comment = 'Black can O-O'
                        elif f == 'e':
                            comment = 'Black can O-O-O'
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

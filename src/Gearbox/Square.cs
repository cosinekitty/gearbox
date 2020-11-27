namespace Gearbox
{
    public enum Square
    {
        Empty    = 0x00,
        Pawn     = 0x01,
        Knight   = 0x02,
        Bishop   = 0x03,
        Rook     = 0x04,
        Queen    = 0x05,
        King     = 0x06,
        White    = 0x08,
        Black    = 0x10,
        Offboard = 0x20,

        WP = White | Pawn,
        WN = White | Knight,
        WB = White | Bishop,
        WR = White | Rook,
        WQ = White | Queen,
        WK = White | King,

        BP = Black | Pawn,
        BN = Black | Knight,
        BB = Black | Bishop,
        BR = Black | Rook,
        BQ = Black | Queen,
        BK = Black | King,
    }
}
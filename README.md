# Gearbox <span style="vertical-align: middle;">[![Build Status](https://travis-ci.com/cosinekitty/astronomy.svg)](https://travis-ci.com/cosinekitty/gearbox)</span>
A chess engine written in C# for all .NET 5.0 target platforms.
A sample graphical interface is provided for Windows.

### Endgame tables
Gearbox includes endgame tables for all possible configurations of up to 5 pieces (including the two kings).

The tables are available here for download in a 9.6GB zip file:

[gearbox_compressed_endgame.zip](https://cosinekitty-gearbox.s3.amazonaws.com/gearbox_compressed_endgame.zip)

SHA256 checksum:
`e1cfe4450f14d7de5ff063c1cf4e21231e19d52e57eeea5d07cab02672ce2a06`

To use these tables with Gearbox, download and unzip that file in some directory on your system.
Optionally, you can then check the integrity of the individual files using the command

```
sha256sum -c egm.sha256
```

Set the environment variable `GEARBOX_TABLEBASE_DIR` to the full path of that directory,
so Gearbox knows where to find the tables.

For developers working on the data compression algorithm that generated the tables above,
the complete raw table data is available in an 11GB zip file:

[endgame3.zip](https://cosinekitty-gearbox.s3.amazonaws.com/endgame3.zip)

SHA256 checksum:
`a5a3931fa75ceca462ce2b644d74bb6ee4b1c565fc29001628e280d91426b29f`


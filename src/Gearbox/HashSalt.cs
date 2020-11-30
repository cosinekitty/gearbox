/*
    MIT License

    Copyright (c) 2020 Don Cross

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

namespace Gearbox
{
    internal static class HashSalt
    {
        internal static readonly ulong[,] Castling = new ulong[16, 2]
        {
            { 0xbe353c51cb8807cbUL, 0xdf660358f987f524UL },  // -- -- -- --
            { 0x9e876b9e4461d1f7UL, 0x4efead44d28bf5e6UL },  // -- -- -- WK
            { 0x0c0e0237f1f2fcc5UL, 0xc27620a64166d2f9UL },  // -- -- WQ --
            { 0xc19078f84ba88b8bUL, 0x35816d847c9b1f3fUL },  // -- -- WQ WK
            { 0xc1c3d634ffc3d510UL, 0x0bee28e524ce48abUL },  // -- BK -- --
            { 0xe51e72251d6e548fUL, 0xd1ca27541a32db5eUL },  // -- BK -- WK
            { 0x6003eb5fb8c9f99eUL, 0x1c3d619af708163aUL },  // -- BK WQ --
            { 0xde9e4ce403b68551UL, 0xfac9524ab49aa380UL },  // -- BK WQ WK
            { 0x43dba6dc21f992c3UL, 0xaa8b2ea185988610UL },  // BQ -- -- --
            { 0x087dfdeec16b33c5UL, 0xd1284080d7b262a9UL },  // BQ -- -- WK
            { 0x15548f81c2de0740UL, 0xd2e70db3c7a38672UL },  // BQ -- WQ --
            { 0xfd44c0bc40b3a216UL, 0x0466a72aed46dffbUL },  // BQ -- WQ WK
            { 0x2539586139bddaa3UL, 0x6f01c41157378740UL },  // BQ BK -- --
            { 0x02f0707b26eb0930UL, 0x4f0103664ca4362aUL },  // BQ BK -- WK
            { 0x9dddf6c789378e50UL, 0x80841908cab5c4e4UL },  // BQ BK WQ --
            { 0xa5369d123648b325UL, 0x16723f0a7bc7c713UL },  // BQ BK WQ WK
        };

        internal static readonly ulong[,,] Data = new ulong[12, 64, 2]
        {
            {
                { 0xb63ce8dd4c28ba30UL, 0x4f5e5957a5e0f51fUL },  // [ 0] White to move
                { 0x3958b88105d71540UL, 0x5b8dc5b065e266b6UL },  // [ 1] (not used)
                { 0x5147a259def510aeUL, 0x566bcaec2f45bfeaUL },  // [ 2] (not used)
                { 0xe91d4900c77dd0a9UL, 0x2bf1be6d5b10f44bUL },  // [ 3] (not used)
                { 0xb58e1caa62fd165fUL, 0xc81d58bb1ec7730fUL },  // [ 4] (not used)
                { 0x8c7d84b1f95e08d6UL, 0xd2bd140e3732653dUL },  // [ 5] (not used)
                { 0x6ab5490297f003c9UL, 0xd177c2de80b648e3UL },  // [ 6] (not used)
                { 0xc831e0d77307fad4UL, 0x6923079c9129476eUL },  // [ 7] (not used)
                { 0x1943608cc462eddcUL, 0xcaed08d78472f94bUL },  // [ 8] P a2
                { 0x68c4ee7e94f88663UL, 0x479a38a82b239f64UL },  // [ 9] P b2
                { 0xf709ab3a0433106dUL, 0x920a22b2ae765d1bUL },  // [10] P c2
                { 0x44649a08b7179c3fUL, 0xa3b0caa64153310aUL },  // [11] P d2
                { 0xcbcfa43ca34156c7UL, 0xc22f712a2b489805UL },  // [12] P e2
                { 0xbbed435b74bb8a5eUL, 0xaa6f2626c55dda5cUL },  // [13] P f2
                { 0xd56cf35f49723481UL, 0x29c74ca553dc4d34UL },  // [14] P g2
                { 0x34f3f418be1bc512UL, 0xce72ca34c9443f2aUL },  // [15] P h2
                { 0x325ff856a3d0c13dUL, 0xec9c0101cd8ee948UL },  // [16] P a3
                { 0x57e7848f00cf92d0UL, 0x249325a4e349e212UL },  // [17] P b3
                { 0x36d5ea3869365554UL, 0xd9188ceb17ce1953UL },  // [18] P c3
                { 0xbe38c98e50f29e6fUL, 0x887227f9896c967eUL },  // [19] P d3
                { 0x4dd1bf59c8064871UL, 0xf2bd25ce3c84cd20UL },  // [20] P e3
                { 0xd02f45403bffa8b1UL, 0x75c82496f717699eUL },  // [21] P f3
                { 0x1797f2079d291576UL, 0x1a25c49cae4abdecUL },  // [22] P g3
                { 0x2b6c6a3755f3d4b7UL, 0x14836f19c79aebe3UL },  // [23] P h3
                { 0x665ffb87769197d3UL, 0x6692491ab7b4cdd1UL },  // [24] P a4
                { 0xd223a263c3bb8260UL, 0x3229462cc7edf7c6UL },  // [25] P b4
                { 0x79b2d50583e7e16eUL, 0x7514eea0a7c22a40UL },  // [26] P c4
                { 0x7f48f26cd1d6a240UL, 0x05389ab8401be9cdUL },  // [27] P d4
                { 0xaf655202ec15fd6bUL, 0x8469084375de6ae7UL },  // [28] P e4
                { 0x3d54c64293219311UL, 0x9c991cdc95d4da1bUL },  // [29] P f4
                { 0x1bafb927036523ffUL, 0xf14dfebc83b79ec5UL },  // [30] P g4
                { 0xbc7c72f141152082UL, 0x07dd186107aab4b1UL },  // [31] P h4
                { 0xf8b42531977bf52dUL, 0x953518539e94b04bUL },  // [32] P a5
                { 0x400f593353b8d23aUL, 0x1c0244ca3b12a837UL },  // [33] P b5
                { 0x6de9222c363daffcUL, 0x14f8c5dce015517bUL },  // [34] P c5
                { 0xed9940fe55eb9ef8UL, 0xcd2dbcbd704296feUL },  // [35] P d5
                { 0xfa250482a13679fbUL, 0xa590e712767df42bUL },  // [36] P e5
                { 0xcfd18aa75a8ee1d3UL, 0x6b2edf4c4e3f65dbUL },  // [37] P f5
                { 0xafabb50d3ef3092bUL, 0xb420d4ff1dc98e3eUL },  // [38] P g5
                { 0x51f61506e4132994UL, 0xaa5e74b81384469eUL },  // [39] P h5
                { 0x317bd619c824c1d0UL, 0x8924bd6737cb57abUL },  // [40] P a6
                { 0xcceb0a98645c5bbfUL, 0xbd251d402cfcb6aeUL },  // [41] P b6
                { 0xd2816d5c061df473UL, 0xe94ff737a464a0aaUL },  // [42] P c6
                { 0x3a918019e5b5128cUL, 0xb0190d256d5588efUL },  // [43] P d6
                { 0x0c9a2438dfcb181aUL, 0xbfaa318058a400f4UL },  // [44] P e6
                { 0x91c216daf73b3e40UL, 0xbec13705b821e5f9UL },  // [45] P f6
                { 0x4f8f1b5151ff2f70UL, 0x209795e8a2ba7061UL },  // [46] P g6
                { 0xaba2a64c53a906e1UL, 0x7a94b642a240892cUL },  // [47] P h6
                { 0xcf1704c72b127220UL, 0x7b3389b932a69364UL },  // [48] P a7
                { 0x1c229977f057f7f1UL, 0x140180442c3f5e29UL },  // [49] P b7
                { 0x73536020b1989549UL, 0x9685392bb1ef0612UL },  // [50] P c7
                { 0x65f0501a398fd669UL, 0xe3653475cdfdf12bUL },  // [51] P d7
                { 0x25efeac9724dbc7fUL, 0x08ddd95e6a50f695UL },  // [52] P e7
                { 0xcafe8b5acd105016UL, 0x238baa6a703470c7UL },  // [53] P f7
                { 0xfe4e90bb92023a28UL, 0x06563aa17a12d680UL },  // [54] P g7
                { 0x5a7ea39b7279719bUL, 0x56fd8100dfaa8765UL },  // [55] P h7
                { 0xd0a4ab177c523ef5UL, 0x704503a472786e7bUL },  // [56] en passant target on a-file
                { 0x6d0398e626570f0eUL, 0xcf2360998c8ef20bUL },  // [57] en passant target on b-file
                { 0x8b318bbfd582a98aUL, 0x1f3b4dec9cad2dabUL },  // [58] en passant target on c-file
                { 0xbba8a5ea4b49f7e1UL, 0x6b01fea40c8731a0UL },  // [59] en passant target on d-file
                { 0x1779fa34f91ed7cfUL, 0x92c80ce2f538ca11UL },  // [60] en passant target on e-file
                { 0x0712383016c3f3b0UL, 0xeb06b229bdff7439UL },  // [61] en passant target on f-file
                { 0xb5b2d59f368b8ab6UL, 0x3124059eec78607cUL },  // [62] en passant target on g-file
                { 0x7ba00d774022c259UL, 0xfea8890271110a96UL },  // [63] en passant target on h-file
            },
            {
                { 0x81e7ded4ff01a3caUL, 0x77ccdc22fd526889UL },  // [ 0] N a1
                { 0x52ad9a12d4ba166cUL, 0xa528a70338f45650UL },  // [ 1] N b1
                { 0x3e494b03bfc4f6a9UL, 0x41cae3412ccc8b2eUL },  // [ 2] N c1
                { 0x93cef0f47ce6683aUL, 0xf8feb5d3db1ca423UL },  // [ 3] N d1
                { 0xe5571215c51baac5UL, 0x2d4d3ca5d8c40017UL },  // [ 4] N e1
                { 0xbba973e0484ac947UL, 0xb5703d344370e754UL },  // [ 5] N f1
                { 0x547340daf6ec4eadUL, 0xc375cea760632aa5UL },  // [ 6] N g1
                { 0xc93f7ef90774b70fUL, 0x7451f7c0f924e6ecUL },  // [ 7] N h1
                { 0x2a9b3540a1c19680UL, 0x761a64aba1e6de6dUL },  // [ 8] N a2
                { 0x557d2ea3c2b1481bUL, 0x68c9cea494ec778cUL },  // [ 9] N b2
                { 0xb8232b22e903c2dcUL, 0x9f2d3a4b24bf7524UL },  // [10] N c2
                { 0xa23df4b9f7169af0UL, 0xfc694ba48d89db03UL },  // [11] N d2
                { 0xbddc5e5a7510b991UL, 0x24e4bb31ac7c39bfUL },  // [12] N e2
                { 0x781b97509ec2aeb2UL, 0x8d674b28b337e378UL },  // [13] N f2
                { 0x60ee2dd0596f5bedUL, 0x9ca74af00e5db0daUL },  // [14] N g2
                { 0xc0afa38f140dc661UL, 0x9c074b36c20a9c2eUL },  // [15] N h2
                { 0x33217a32d233450aUL, 0xb216497e3864666cUL },  // [16] N a3
                { 0x8c13e0f3ba8834fbUL, 0x98617c8f70b6b1dbUL },  // [17] N b3
                { 0xdb41abba50a3cabfUL, 0x4028a9580dc1df99UL },  // [18] N c3
                { 0xda0556a5cc247f2eUL, 0xc429687ca7b644e4UL },  // [19] N d3
                { 0x58de10633deb9a9aUL, 0x22ff1550bf59b741UL },  // [20] N e3
                { 0xfafeac02b25c4743UL, 0x46a1ceef767e8ff3UL },  // [21] N f3
                { 0x2af43f6b609c2f39UL, 0x8fa997fc40627357UL },  // [22] N g3
                { 0x5fe4ccb376960e5bUL, 0x0453f3112553959fUL },  // [23] N h3
                { 0xb09e5111304e69d0UL, 0x532198190899bbb2UL },  // [24] N a4
                { 0xbdc25cafe4ec64a1UL, 0x75c2887e6b4c4693UL },  // [25] N b4
                { 0x6d467d1728538bb2UL, 0xd961ef4ddeba2582UL },  // [26] N c4
                { 0xa1e0aa03df2f0517UL, 0x83aedd46b78d9a97UL },  // [27] N d4
                { 0xe9e2d317755d4f6cUL, 0x5ff6ebc90fb449d5UL },  // [28] N e4
                { 0x15e7be657c744bbeUL, 0x07710a1176183edbUL },  // [29] N f4
                { 0x8913d49de6e5ff51UL, 0x4fef264f45c49f78UL },  // [30] N g4
                { 0xf82a093e638cebc0UL, 0x5b322c53b21977d7UL },  // [31] N h4
                { 0xe673c558eab72897UL, 0xfcef9dfd166ac134UL },  // [32] N a5
                { 0x9731c375b0ac57a1UL, 0x9a4416cc40321b1dUL },  // [33] N b5
                { 0x79e0a7f90edd7acdUL, 0x2954c8b26d7ef0f8UL },  // [34] N c5
                { 0x80a7c89c3ff86099UL, 0x502ddf9c70580c59UL },  // [35] N d5
                { 0x0c261926c3c24054UL, 0x502f6ac17ee19e2fUL },  // [36] N e5
                { 0x964c0574ca33c8e7UL, 0x1735ca234bae9654UL },  // [37] N f5
                { 0xec4cb2e8fd22b7feUL, 0xcc73ed78362ec534UL },  // [38] N g5
                { 0x8d8fef5430831ef7UL, 0x64339f160c9e9133UL },  // [39] N h5
                { 0x72993522a360bd2dUL, 0x09d8323d7507c132UL },  // [40] N a6
                { 0x75921a42874369b0UL, 0x49133d25358dd168UL },  // [41] N b6
                { 0xc5d8051a3492da52UL, 0xd04a79593cac9762UL },  // [42] N c6
                { 0x1ea58fd24f59cc22UL, 0xe9d34236cef6ebc6UL },  // [43] N d6
                { 0xded25f9e9f97a1c1UL, 0x49fdafac35d73471UL },  // [44] N e6
                { 0x0352bf258d1ff4d3UL, 0xbadef82654623c49UL },  // [45] N f6
                { 0xddb917c5d174658bUL, 0x9c5887600e13d5ecUL },  // [46] N g6
                { 0xa7c264879268d1cdUL, 0xb76bda15854f2fe8UL },  // [47] N h6
                { 0x0c5dcaf0f5b3d290UL, 0x3248b5363bc7adc7UL },  // [48] N a7
                { 0xdfa7c05283c72a20UL, 0xca783f7343d0d8d3UL },  // [49] N b7
                { 0xda1a7a87d6b68886UL, 0x5aec96e73ed28c16UL },  // [50] N c7
                { 0xe2ae56147f1a89e7UL, 0x6a559a4bfb4c6127UL },  // [51] N d7
                { 0x30da096bc2f6dcc2UL, 0x85b02dcac0e7b18aUL },  // [52] N e7
                { 0x54a40c423b37ea04UL, 0x7863af008f2963a6UL },  // [53] N f7
                { 0x81af7c13d4a45598UL, 0xc5bb634140a3d439UL },  // [54] N g7
                { 0xe78271c3f16e028fUL, 0x5b3137bb35936a77UL },  // [55] N h7
                { 0xee11446cb12f0119UL, 0x28cfff580fb82b51UL },  // [56] N a8
                { 0x59bd757193a48cf1UL, 0xfb0e35a49b6e20c4UL },  // [57] N b8
                { 0xaa4d36c8f9285d6bUL, 0x53aee75099d7105bUL },  // [58] N c8
                { 0x4ca6a429228099a7UL, 0xecf4bb1420807dc6UL },  // [59] N d8
                { 0x2f473f81c4b58e48UL, 0x9b0451060413a2d1UL },  // [60] N e8
                { 0xefaa3f577b48e54cUL, 0xaffc4e9a75fd2c4bUL },  // [61] N f8
                { 0xc9552d45e4d32c5cUL, 0xa496e0d6a24802d3UL },  // [62] N g8
                { 0x9c9664a194a83ffcUL, 0x0ffd3396026dcbcfUL },  // [63] N h8
            },
            {
                { 0xf12c0088c6ec65b8UL, 0x92466d59aa967fcbUL },  // [ 0] B a1
                { 0xde2fdac0e63071cbUL, 0x1c29ead79d408a7cUL },  // [ 1] B b1
                { 0x780765488b00c17bUL, 0x79d4b2609cab12caUL },  // [ 2] B c1
                { 0x00795bd08f032343UL, 0xbe6e61442b4e9e03UL },  // [ 3] B d1
                { 0x26cc00a0599add04UL, 0x4275eef4e2c2b99eUL },  // [ 4] B e1
                { 0x2102685d093974cbUL, 0x523f61a4a0d06a0dUL },  // [ 5] B f1
                { 0x5f9a9d618da17e67UL, 0xebe3a2d72021389eUL },  // [ 6] B g1
                { 0x440e766a87b183f1UL, 0x0a6d7f5df9a5cd8aUL },  // [ 7] B h1
                { 0xbb62a46d17eed348UL, 0x87b0b24936063e7dUL },  // [ 8] B a2
                { 0x2f0082054e68b448UL, 0xfb80b026dda1b1a5UL },  // [ 9] B b2
                { 0x66ff76aa6b2c95ceUL, 0x68bfe4e06294af8eUL },  // [10] B c2
                { 0x29bb9d3bc5f51b2eUL, 0x33f6ff4405c6a88cUL },  // [11] B d2
                { 0x56bd4611796cc5efUL, 0xa41228d4f45d0dd4UL },  // [12] B e2
                { 0xbdb0998605a6e909UL, 0x82e4d3cd6e1d3baaUL },  // [13] B f2
                { 0x9b9ded941546bc1aUL, 0x8d91d0ff78240222UL },  // [14] B g2
                { 0xa288494e78ea2b64UL, 0xfa33779abe50b647UL },  // [15] B h2
                { 0x72a23b22f8eeccabUL, 0x612d2775e9113a82UL },  // [16] B a3
                { 0x22d756e296119799UL, 0x3bcfca9368f9e7ceUL },  // [17] B b3
                { 0xe48fe5856da87b8fUL, 0x66072e7f38a39c09UL },  // [18] B c3
                { 0xc67d9ea469b9b00dUL, 0x6bd3a104640e778bUL },  // [19] B d3
                { 0xe37e253bb80709ceUL, 0xe52a748fcbcee1b5UL },  // [20] B e3
                { 0x379531ef1768f060UL, 0xb5e2fa1310402b9eUL },  // [21] B f3
                { 0xe671296b753cff62UL, 0xcd5bca244b5c5ebdUL },  // [22] B g3
                { 0x82ebede88c263a2dUL, 0x0fde5b3d1cd1c938UL },  // [23] B h3
                { 0x70a4b875252a0d61UL, 0x688dab3f55a84f42UL },  // [24] B a4
                { 0xe540057cef517cf3UL, 0x31541a612712dbaaUL },  // [25] B b4
                { 0x456900496ca20443UL, 0x8ef95394e370ba81UL },  // [26] B c4
                { 0x04ada5975be0ed74UL, 0x99cee783893472dcUL },  // [27] B d4
                { 0xd55912d4c070ec9dUL, 0x55f09cd94ce79e8eUL },  // [28] B e4
                { 0xf94dcf5afcefe490UL, 0x0d0907432e0cb39bUL },  // [29] B f4
                { 0x49272907896426c3UL, 0x31dc9dd29cd0b0aaUL },  // [30] B g4
                { 0x650457b28da515e1UL, 0x6ab2b6277a5edffdUL },  // [31] B h4
                { 0x1a0c03bce40f7f25UL, 0xd43b2c57ec5dcb61UL },  // [32] B a5
                { 0x71b5556e938889afUL, 0xc29719f33c19cfceUL },  // [33] B b5
                { 0x5ff6b4f19b6c12b8UL, 0x988f93caf4bd8848UL },  // [34] B c5
                { 0x504a394fba68d9bfUL, 0x1ec953a4b58dc724UL },  // [35] B d5
                { 0xabffd791823d1488UL, 0xb2972f9934e0da71UL },  // [36] B e5
                { 0x33ba922dc66bd9d3UL, 0x3da2f225dfd5d61fUL },  // [37] B f5
                { 0x2a45e50a52d9d43cUL, 0xbadf495f60bf8de4UL },  // [38] B g5
                { 0x4c8ab3e44b46e254UL, 0xc59055ccf7475352UL },  // [39] B h5
                { 0xa25594427b55f15eUL, 0x0a57ae6c74b3f010UL },  // [40] B a6
                { 0x8e6b9781732d09f2UL, 0xb321060bbe9bde6cUL },  // [41] B b6
                { 0xf406f632a15ea9e4UL, 0xfd9ad4435c28319fUL },  // [42] B c6
                { 0x67d667c1604af16cUL, 0x178a7b333c39d860UL },  // [43] B d6
                { 0xefe0845979cb1fd1UL, 0x88a18e6c8468bfe3UL },  // [44] B e6
                { 0x4bbd8dfc819bb159UL, 0x3ed6957b5f2b29b4UL },  // [45] B f6
                { 0x8e6bf8202762a087UL, 0x13014664268244f5UL },  // [46] B g6
                { 0x253ab5e5034008fdUL, 0x29f45359176ccd47UL },  // [47] B h6
                { 0x9a3020a9c0a71fb2UL, 0x4716f8cbc38d9889UL },  // [48] B a7
                { 0xa3d1b85ff297e94dUL, 0xaf74689ac3108b00UL },  // [49] B b7
                { 0xe73471a83baa4f54UL, 0xcae8d5ae7c2d7af1UL },  // [50] B c7
                { 0x21c4191311936b45UL, 0x21cace6af0e3a068UL },  // [51] B d7
                { 0x25ac67cfce695d45UL, 0x10187ecb8598107fUL },  // [52] B e7
                { 0x6edb91eb15e004c6UL, 0xc87bf5cb863d3e77UL },  // [53] B f7
                { 0x96a6e3a4e86ddc70UL, 0x0020226db0d045e4UL },  // [54] B g7
                { 0x4f780d1e98527afeUL, 0xad9de67b99a0c3baUL },  // [55] B h7
                { 0x6a03540fffae9217UL, 0x1d9ff2a4bb5ed972UL },  // [56] B a8
                { 0xa78e559dbd8a18fcUL, 0x81a0cc7c09c6f7a2UL },  // [57] B b8
                { 0x8ab204a6ca8e94c3UL, 0xa062a6b3ec805aa5UL },  // [58] B c8
                { 0x01a8d3a3511b9065UL, 0x5886127482d25276UL },  // [59] B d8
                { 0xd315c1ad825dd93aUL, 0xd27e119ee1735ceeUL },  // [60] B e8
                { 0x6503c0fbb3ad8af5UL, 0x155ef261abb1428fUL },  // [61] B f8
                { 0xea13e4a2797ffec1UL, 0x1f074266c3e2b5f0UL },  // [62] B g8
                { 0xfb84867ff65e0ca9UL, 0x302da4f9b37dfda1UL },  // [63] B h8
            },
            {
                { 0x53a2d03b060651b7UL, 0x9a5dfe6130c2fcd0UL },  // [ 0] R a1
                { 0x0fbd3ac98723f160UL, 0x51dd1a02a1e384daUL },  // [ 1] R b1
                { 0x6a0276775a1eed0eUL, 0x1b202944db531587UL },  // [ 2] R c1
                { 0x5a81e90701b55f78UL, 0xca670ddee277ac8dUL },  // [ 3] R d1
                { 0x9a32b2dfc0b59ad5UL, 0x18ca62fa55be32b9UL },  // [ 4] R e1
                { 0x9cde5881d74e5884UL, 0x71af2a9d038d1c39UL },  // [ 5] R f1
                { 0x6f4e019de93ea74bUL, 0x88823a36d7e10f26UL },  // [ 6] R g1
                { 0xf40d4932cd9510baUL, 0xdd4750a558e78993UL },  // [ 7] R h1
                { 0xdd5b00b581a14688UL, 0xb3610d3c86b6ca18UL },  // [ 8] R a2
                { 0x14a37c67d15d33f6UL, 0xe1bd24621a2789afUL },  // [ 9] R b2
                { 0x9bdbd37b84e678a2UL, 0xd9430a18b0f2fbc9UL },  // [10] R c2
                { 0xf0d9981490dafc09UL, 0x9257bdba17f7d807UL },  // [11] R d2
                { 0xa4434f16147cf188UL, 0x23ab468e87d7c6fbUL },  // [12] R e2
                { 0x0f7948be4e366f43UL, 0x3a493f1c778e8797UL },  // [13] R f2
                { 0x94a46e17d458a7feUL, 0x09cdaa6cdd6a2ef9UL },  // [14] R g2
                { 0x2962627de6b0f053UL, 0x50d7f65eb879e8ceUL },  // [15] R h2
                { 0xbd8ceeb99d6b9625UL, 0xb4b5ad9657953da3UL },  // [16] R a3
                { 0x0286691a3a6ede59UL, 0xfe5c3bf11c1d0d4aUL },  // [17] R b3
                { 0xcef03e54be7ebe02UL, 0x2530ed59a674a014UL },  // [18] R c3
                { 0x930c4221449f8317UL, 0x8acc7a03b44a7171UL },  // [19] R d3
                { 0xe882baeb5ab7a164UL, 0xc161b6700990aa60UL },  // [20] R e3
                { 0x065c18a4b2a1f83eUL, 0x62a632141b1a60e1UL },  // [21] R f3
                { 0xff928cf69fcbd766UL, 0x11074bb71cc0ea4cUL },  // [22] R g3
                { 0xc85a1eb62ef6ee5fUL, 0x4c7f38b0591f8873UL },  // [23] R h3
                { 0xb3d159a36e0840d7UL, 0x7217f320e4b2d3e9UL },  // [24] R a4
                { 0x147636a514b06ce6UL, 0x412d90f56e05e97cUL },  // [25] R b4
                { 0x2182cf3976ceec90UL, 0x97b28dae255302afUL },  // [26] R c4
                { 0x9d773eea035b6876UL, 0xdbb6ed0d38de8704UL },  // [27] R d4
                { 0xb4a4917980958b4bUL, 0xc5e1eef7439d57e3UL },  // [28] R e4
                { 0x2fa0036d3140a8c9UL, 0x725cb2f9f5ce8610UL },  // [29] R f4
                { 0x95813c6c0e30ec13UL, 0x3a2cfbba6ec90ccaUL },  // [30] R g4
                { 0xace230cc9e233e11UL, 0xd4aa66ba1ecb6946UL },  // [31] R h4
                { 0xe114500d37a02522UL, 0x23e1aef1f010e234UL },  // [32] R a5
                { 0x76cda0a65bb36b1dUL, 0xce14251fde57ab25UL },  // [33] R b5
                { 0x298840949003b341UL, 0x7d7fc1be6de0c2bcUL },  // [34] R c5
                { 0x5fe6ee664fa9d1a1UL, 0xcc5bb0eff4a9083fUL },  // [35] R d5
                { 0x0f74f435900a61bcUL, 0xa37fd244b06c5414UL },  // [36] R e5
                { 0xab22016aabbee65cUL, 0x8d938c27c4a792a6UL },  // [37] R f5
                { 0x19db5e8bdf5f404dUL, 0x1a70d162dba45446UL },  // [38] R g5
                { 0x4fffcff95b5749c2UL, 0xbb151bb55d44441eUL },  // [39] R h5
                { 0x64a6ebfbaf1f4846UL, 0xae81973f4b57841cUL },  // [40] R a6
                { 0x657b6393506cc966UL, 0x631a80cb7cffda9dUL },  // [41] R b6
                { 0xc6f41f12d04c2c8cUL, 0x2c6e7e0c44998ce4UL },  // [42] R c6
                { 0xf60fa7c02dcd70eeUL, 0x120db78e1227aec8UL },  // [43] R d6
                { 0x2d44c137a85564e3UL, 0xcdbeec304c9da019UL },  // [44] R e6
                { 0x5d105920454ec944UL, 0x312dde91e5764952UL },  // [45] R f6
                { 0xad4a95b144bb7024UL, 0x7d4c69433dd87c4bUL },  // [46] R g6
                { 0xe2064db9a2db607bUL, 0xaf52c2ad18b5f58fUL },  // [47] R h6
                { 0x506b443abce2af8bUL, 0x7dfdaa45e55fda3dUL },  // [48] R a7
                { 0x927e3af79e889e75UL, 0x1f18f91060c79bf9UL },  // [49] R b7
                { 0xbf816e32c9c7561bUL, 0x7ec5cde3c68d7419UL },  // [50] R c7
                { 0x7feaa185a406aa27UL, 0x09f3b9267b63d725UL },  // [51] R d7
                { 0x62246108598786c1UL, 0xc6efccdff2652ee9UL },  // [52] R e7
                { 0xd012f99a308a59bcUL, 0x5ca4033ba4d2a1a1UL },  // [53] R f7
                { 0x5fbec0071f16fcb5UL, 0x46040f109d9e1de2UL },  // [54] R g7
                { 0xc80c379dc213c647UL, 0x1a7a1ffe253b8745UL },  // [55] R h7
                { 0x5dc08bd3dd7bf34fUL, 0xa41bb531cd26b83bUL },  // [56] R a8
                { 0xb68642e9ed13d728UL, 0xa5211eb0874f5599UL },  // [57] R b8
                { 0x4e22cb6a38a0535eUL, 0xedea45d4700dd5a6UL },  // [58] R c8
                { 0xc26197b91a8d2b35UL, 0x0b7918b81c6f5724UL },  // [59] R d8
                { 0x3bd396b59a69b3acUL, 0x2d4ae5cc9fe51cb1UL },  // [60] R e8
                { 0xfb4e1867fb9d7372UL, 0x19b76f1f925fbf07UL },  // [61] R f8
                { 0xc8556eb0113fa856UL, 0x3ffa7cdefbc0707cUL },  // [62] R g8
                { 0x9776e0e9fa91e936UL, 0xfff38192eccef57cUL },  // [63] R h8
            },
            {
                { 0x2ef4d2d344199910UL, 0xc9383345cd281f99UL },  // [ 0] Q a1
                { 0x94b726692ef379f4UL, 0xea05334ef5e10dcaUL },  // [ 1] Q b1
                { 0x689927a9d5387043UL, 0x668e334c57cf2c00UL },  // [ 2] Q c1
                { 0xb5a5179039449ae9UL, 0x5b24b78a3b339497UL },  // [ 3] Q d1
                { 0x1d20363a267b2d08UL, 0x2cab8bb864edac90UL },  // [ 4] Q e1
                { 0x11e36c4d1c742251UL, 0x5bb3116e399e39a2UL },  // [ 5] Q f1
                { 0x2a85bd57d5ca2ec3UL, 0xdc1c4dc46b979f02UL },  // [ 6] Q g1
                { 0x56d6d477b22ad162UL, 0xb7c66b6f1e438c62UL },  // [ 7] Q h1
                { 0x65f62469d6f7134dUL, 0x50d15edced5b519dUL },  // [ 8] Q a2
                { 0x8365229e43b814caUL, 0x4c1cd58daa167e44UL },  // [ 9] Q b2
                { 0x5c76eb30c5e330beUL, 0x2a7614069b9e4b40UL },  // [10] Q c2
                { 0x2f605d5912770fb1UL, 0x251758a443629b80UL },  // [11] Q d2
                { 0xebd44112457dddd4UL, 0x6803841c6df079b9UL },  // [12] Q e2
                { 0x6941ec3491d42130UL, 0xcefdc5aa66c8561aUL },  // [13] Q f2
                { 0x9a1a1e0930ad9d7bUL, 0x390022e8a0f2d8b3UL },  // [14] Q g2
                { 0xbbcea0df54ed5674UL, 0xa451afedd0e376ceUL },  // [15] Q h2
                { 0x8b5f0256f57af61dUL, 0x7783bd19ecbe806bUL },  // [16] Q a3
                { 0xe3a642145ae93b13UL, 0x81995ac883803e14UL },  // [17] Q b3
                { 0xf9ad7e6f86d58294UL, 0xa63774dd192eedb3UL },  // [18] Q c3
                { 0x04300649d8aeca34UL, 0x171f1a063f835c69UL },  // [19] Q d3
                { 0x32b485aa9e91005bUL, 0xb5c36d8fb0919ba7UL },  // [20] Q e3
                { 0xd708dcf002148665UL, 0x482bf0ec27559822UL },  // [21] Q f3
                { 0xd12883d51a5ecee5UL, 0x26644ff46e150940UL },  // [22] Q g3
                { 0x2db9233bda22775cUL, 0x643053e3aa9ad440UL },  // [23] Q h3
                { 0x1960818ff826eb20UL, 0x9796996d0612b082UL },  // [24] Q a4
                { 0x7141159cf665fb76UL, 0xde4b94c50086d86aUL },  // [25] Q b4
                { 0x74e49ebc1ee044b3UL, 0x93ad7d4c178295c5UL },  // [26] Q c4
                { 0xf0b40ba7dfa4ad7cUL, 0xd7ddb691abcb2de0UL },  // [27] Q d4
                { 0x5e02f979baf75ef4UL, 0xaad6b0f902d5e11bUL },  // [28] Q e4
                { 0x2f9b6a7bfe660d8dUL, 0x988977cb793a2778UL },  // [29] Q f4
                { 0x02e6c96af98442d7UL, 0xc8a16d148e53d371UL },  // [30] Q g4
                { 0xee4c517981c61597UL, 0xcd9404b138310c1cUL },  // [31] Q h4
                { 0xc4d86e7a68184752UL, 0xd8d652132000323fUL },  // [32] Q a5
                { 0xfecf9e680e4d2a06UL, 0x01bbfb4ab46f4b68UL },  // [33] Q b5
                { 0x40534360e1971b3dUL, 0x64f31bc3c84745c2UL },  // [34] Q c5
                { 0x6f3430961a79e45bUL, 0x98a76e65c582ae37UL },  // [35] Q d5
                { 0xabadfaae735351a8UL, 0x5751d5d1c9fcc3f6UL },  // [36] Q e5
                { 0xf0d27586f2bbb5ebUL, 0x952d48eb49368b69UL },  // [37] Q f5
                { 0xf0a19f48a36c7a6eUL, 0xd456f52fe62a464eUL },  // [38] Q g5
                { 0x571358b28db3bcfdUL, 0x680fcb4a00b2d054UL },  // [39] Q h5
                { 0x9f1d3b59a314c293UL, 0x831bd46299a23204UL },  // [40] Q a6
                { 0x5cd9948e1763d243UL, 0x85a6d3739372d678UL },  // [41] Q b6
                { 0x2998106efdfe0906UL, 0x78fafa4c0ec33e63UL },  // [42] Q c6
                { 0x05e7e644fa7a0ce0UL, 0xa4d9fb75e3c36879UL },  // [43] Q d6
                { 0xa09d57ab4af46c27UL, 0x496f2ab838149497UL },  // [44] Q e6
                { 0x77a32bbf1a40aaf6UL, 0xb6d50f8cedc946ecUL },  // [45] Q f6
                { 0x4e038aadd7a36fe5UL, 0xb9676428468d50f3UL },  // [46] Q g6
                { 0xf9b79ea9523a7662UL, 0x3b04ce72dfd18edfUL },  // [47] Q h6
                { 0xf586e95460be374cUL, 0xe52c9934fdca9f39UL },  // [48] Q a7
                { 0x9a3b962d09517b0fUL, 0x17532d6069562c5dUL },  // [49] Q b7
                { 0xdf11b89149fd1357UL, 0x8a5b796231643fe8UL },  // [50] Q c7
                { 0x14bc57f554793001UL, 0x9667c862b9555203UL },  // [51] Q d7
                { 0x72b3b3f17439bd5bUL, 0x773d213cbab0c9a6UL },  // [52] Q e7
                { 0x5c92d09ef024e77eUL, 0xbcdd3a532d5de8e7UL },  // [53] Q f7
                { 0x2e3dec7e6c89470eUL, 0x4f45e44af7be4f05UL },  // [54] Q g7
                { 0x0233a3c6223630dfUL, 0x2a6202407decd700UL },  // [55] Q h7
                { 0x28089bd083c7d10cUL, 0x437b64b92beb55a0UL },  // [56] Q a8
                { 0x4b955a343a93d7adUL, 0x0aed3bb51eb1009bUL },  // [57] Q b8
                { 0xb193e12f806ca925UL, 0xfc8c2cdabfab5e5cUL },  // [58] Q c8
                { 0x941b32902a9e353aUL, 0x66a8f0c78c03b3d8UL },  // [59] Q d8
                { 0x753b807a286e731aUL, 0xc17ce76952fcd406UL },  // [60] Q e8
                { 0x0ed997eb858196f2UL, 0x71ae2719ff75058aUL },  // [61] Q f8
                { 0xa8e3b6a53ce8d314UL, 0x84e7af2932a57de2UL },  // [62] Q g8
                { 0xb2b527828bbf023fUL, 0xe23a68d48360e9a3UL },  // [63] Q h8
            },
            {
                { 0xe56c74ca4755692cUL, 0x024af6b4f413292cUL },  // [ 0] K a1
                { 0x74e0d1202dd29e5fUL, 0xb451056f59393611UL },  // [ 1] K b1
                { 0x432a6cec1fe5dd4cUL, 0x9749c37242919939UL },  // [ 2] K c1
                { 0xe5c74fdff43cffe5UL, 0x659441a750442047UL },  // [ 3] K d1
                { 0x64f8ec931971304eUL, 0x452ad701c643a9daUL },  // [ 4] K e1
                { 0xa51ae4a1449f2ef4UL, 0xf21e1694eba2b34aUL },  // [ 5] K f1
                { 0x88ad799ce81894ecUL, 0x9455109b49f388e3UL },  // [ 6] K g1
                { 0xb1c1bd25de4da8a5UL, 0x21f1416d8ed3a4f3UL },  // [ 7] K h1
                { 0xfb6f49396f258ae3UL, 0x949d4708ba0b8f25UL },  // [ 8] K a2
                { 0xf44ae6bb631a4007UL, 0x6a3b63982f81d597UL },  // [ 9] K b2
                { 0x1ebc5b9a96cf13dbUL, 0xb38b8e1a077ef448UL },  // [10] K c2
                { 0xc6f17c1e4add13abUL, 0x897e691378490c3aUL },  // [11] K d2
                { 0xe48762bf1b5cebd7UL, 0x9f7958931b97550bUL },  // [12] K e2
                { 0xe571610643b8927aUL, 0xd885b96a95e79ff9UL },  // [13] K f2
                { 0x35843788b87cab28UL, 0xf6c623f2b0294095UL },  // [14] K g2
                { 0xbcd9565090010699UL, 0xa025de2c14dd88afUL },  // [15] K h2
                { 0x8a858da19348fed4UL, 0x540fa98456d2592aUL },  // [16] K a3
                { 0x7414f99cfabf3be6UL, 0xe35c353fdf6b6634UL },  // [17] K b3
                { 0x834d9b3a592cf7efUL, 0x1fab2375cb6b2b4cUL },  // [18] K c3
                { 0xf08fef3108bb6f36UL, 0x7b90e78746166081UL },  // [19] K d3
                { 0xf762133af6f19757UL, 0xdeb7805d26f94ecfUL },  // [20] K e3
                { 0x4a866d237d7e6593UL, 0x9b5e50073488431eUL },  // [21] K f3
                { 0xf8ab68f4a9e54296UL, 0xedbf2facf3dc7a17UL },  // [22] K g3
                { 0x43488a6210994146UL, 0x081df341bc4fb4b5UL },  // [23] K h3
                { 0x1269b7de33b8eaf3UL, 0xfc6b2ac287c1ae45UL },  // [24] K a4
                { 0xe16598f503029546UL, 0x72e167d0ba3bdb1eUL },  // [25] K b4
                { 0x7d3ebda1c377930bUL, 0x50764e3463e6c6a5UL },  // [26] K c4
                { 0xe0e6fec5bfb73a4eUL, 0x3e96bec8e26ae2d3UL },  // [27] K d4
                { 0x852a162dfa6653deUL, 0x208add70076a9a75UL },  // [28] K e4
                { 0xaf606b49efce7f8eUL, 0x5ec4d6dd6665aa6eUL },  // [29] K f4
                { 0x04744aff11864afaUL, 0x809face5d411dc42UL },  // [30] K g4
                { 0xe03a170fa3c50773UL, 0x8f4d1cddfaf76b38UL },  // [31] K h4
                { 0x8550e68e1b1e4528UL, 0x8e5067d743fbe887UL },  // [32] K a5
                { 0x2071a084a0b919deUL, 0x725f75aaafcc8e30UL },  // [33] K b5
                { 0x5a99370851f38792UL, 0xd8ee37607f885257UL },  // [34] K c5
                { 0xc8faf9f18f459082UL, 0x0306e93ab1264855UL },  // [35] K d5
                { 0xc52a30c24922a1e7UL, 0x6e47eb9afc4d6405UL },  // [36] K e5
                { 0x8f217f2fbcd30891UL, 0x1cb5d9dab1f903f0UL },  // [37] K f5
                { 0xe2bfbe9ac8e8765dUL, 0x26cae520d079c511UL },  // [38] K g5
                { 0x0ecd727f87db36d6UL, 0xc53b80e178dfecc8UL },  // [39] K h5
                { 0x6e55deb29c932c8aUL, 0x15b876183f66de55UL },  // [40] K a6
                { 0xdab3f4feba7dc058UL, 0xb8161a432cfd0fdbUL },  // [41] K b6
                { 0x6dcbe0525321190dUL, 0x9679452deea65f27UL },  // [42] K c6
                { 0xe675a5a4af62e062UL, 0xc54001cb7e50a5c5UL },  // [43] K d6
                { 0x3b1021d11f794d8aUL, 0x909a274881d26757UL },  // [44] K e6
                { 0x87ebeaffd1a85946UL, 0x1bcdf15d73d247e0UL },  // [45] K f6
                { 0xaf2636d64856c172UL, 0x259531d347a28173UL },  // [46] K g6
                { 0x9536f89027e9c38dUL, 0xbf13c22a38ed61d4UL },  // [47] K h6
                { 0xfb7632df198e8980UL, 0x567780fc54ea4836UL },  // [48] K a7
                { 0x3496d617708b9992UL, 0x4302fb8f304d53abUL },  // [49] K b7
                { 0x447220cb8aafad6aUL, 0xd87d49bac3066c4eUL },  // [50] K c7
                { 0xc6c3b3a69f996632UL, 0x73fff8d592faa338UL },  // [51] K d7
                { 0x50b299b70e6d160cUL, 0x0d6b2e5d9e35f61cUL },  // [52] K e7
                { 0x4a30928717aead90UL, 0x04dfdc4d3f583078UL },  // [53] K f7
                { 0x7503b203859f04b6UL, 0x8f1a669c5c0e0bb3UL },  // [54] K g7
                { 0x11268f841b6470efUL, 0x4476805b3a23a160UL },  // [55] K h7
                { 0xb12c46f5acfd0d74UL, 0x522d70e84425d38dUL },  // [56] K a8
                { 0x9b7cad145a370169UL, 0xf76584be97b58305UL },  // [57] K b8
                { 0xd069626069c7e8efUL, 0xaf37bb2de6d1825fUL },  // [58] K c8
                { 0x303829678f964b3aUL, 0x6e9934459debd444UL },  // [59] K d8
                { 0x830b12bc835c06edUL, 0x79591d0daa93c39aUL },  // [60] K e8
                { 0x02608629e848e6d9UL, 0xdd06b566f029a45cUL },  // [61] K f8
                { 0xc269871c76c6eba5UL, 0x1b8173625b4d4ee6UL },  // [62] K g8
                { 0x81cea0ecb43c2cfaUL, 0xd11d5ccab06bf888UL },  // [63] K h8
            },
            {
                { 0xdb6ff850d21e9893UL, 0xa6565b6697ada8a5UL },  // [ 0] (not used)
                { 0xd6fe730ecc7e69caUL, 0x0fc5f5ccdb2cf57eUL },  // [ 1] (not used)
                { 0x557431b739d20fc1UL, 0xb94b41b25bc70c68UL },  // [ 2] (not used)
                { 0x60667493f3cd525aUL, 0x6496c7074794327eUL },  // [ 3] (not used)
                { 0xa64efd0a504adc6aUL, 0xa1f80796695d2022UL },  // [ 4] (not used)
                { 0x41fc7f6d3e796ed0UL, 0x0f5db4ee4007bf56UL },  // [ 5] (not used)
                { 0x4fb5cbaf152939c7UL, 0x82572031686a6c09UL },  // [ 6] (not used)
                { 0xf7c88ba71ddad8fdUL, 0x34c148528dfb9d29UL },  // [ 7] (not used)
                { 0x73ba4c1b7019aab3UL, 0x5f6c7640a84c9b0dUL },  // [ 8] p a2
                { 0x06908779b83056b4UL, 0x775a16ecb84fd49eUL },  // [ 9] p b2
                { 0x3bfc0b8a1aad4f5eUL, 0x9108852d647eec5cUL },  // [10] p c2
                { 0xddec165f113eeb41UL, 0x40e33cfb03c36796UL },  // [11] p d2
                { 0xd418849d9b8d46b7UL, 0xe5e6c9aeb7f2f542UL },  // [12] p e2
                { 0x72f8e714106deac5UL, 0x6eb9d56ffa77605fUL },  // [13] p f2
                { 0x0927b883a23cf7b9UL, 0x747b646a9fd0b556UL },  // [14] p g2
                { 0x6d19bb23133080c3UL, 0x66ad0c62d8f4a7f1UL },  // [15] p h2
                { 0xe399058a9e03e53cUL, 0xa35f4054178403a8UL },  // [16] p a3
                { 0x43e356c0e900c160UL, 0x6dcaaaa268da538dUL },  // [17] p b3
                { 0x3125f24936f21c94UL, 0x73aa7c7d66398743UL },  // [18] p c3
                { 0xe6aad47677811c83UL, 0x33a469bb0beeeb13UL },  // [19] p d3
                { 0x32724c36501f3600UL, 0xa630540bd2b50111UL },  // [20] p e3
                { 0xaabb402d33b06a14UL, 0x40dfc93c5d7a50bfUL },  // [21] p f3
                { 0x5effcd29c11d7f48UL, 0x4acc0befae0cb98aUL },  // [22] p g3
                { 0xacf1f150b2b8ebd0UL, 0xc6e5df9d074a00beUL },  // [23] p h3
                { 0xc09e4b3a74064f20UL, 0x390ed8ac24701a9cUL },  // [24] p a4
                { 0x698d125fb47ff337UL, 0x62d7995d4740a643UL },  // [25] p b4
                { 0xe1a23c526dd5d874UL, 0xdeb8258d7ac87e45UL },  // [26] p c4
                { 0x1d9dc53e52d65b88UL, 0x88d9ced732fe8ea4UL },  // [27] p d4
                { 0x3b68b7f7dcbd3a1fUL, 0x905a48ae764e3c2bUL },  // [28] p e4
                { 0x75f49cd99259c8f3UL, 0xc0f80b8ba5dcd43dUL },  // [29] p f4
                { 0x5241ca5b9c542d8fUL, 0xf9bcefa9cf104207UL },  // [30] p g4
                { 0xedb5effb00763d29UL, 0xae61938da3efade6UL },  // [31] p h4
                { 0x5c549cefa67d39a9UL, 0x8d94f642d662869aUL },  // [32] p a5
                { 0x37f1f6397ac11aa3UL, 0x886e7e876b61ea8eUL },  // [33] p b5
                { 0xee82d340072828daUL, 0x6ba867e3514c72ddUL },  // [34] p c5
                { 0x11410e4914868bdfUL, 0xa9ec42b695a8c717UL },  // [35] p d5
                { 0xf8ab20ed90f49fd0UL, 0xb2e1bbf143382461UL },  // [36] p e5
                { 0x409896fb50983d41UL, 0x56292493f6a93bb8UL },  // [37] p f5
                { 0x9f7fe906edba048aUL, 0xee25dd81532464aaUL },  // [38] p g5
                { 0xb32057a5fa86518eUL, 0xc5c18d821d87cb30UL },  // [39] p h5
                { 0xbbc1ba4947d2387bUL, 0x911e6e9d867d95beUL },  // [40] p a6
                { 0x2d0e84ebaad67bdcUL, 0x085e36c438c4d1a6UL },  // [41] p b6
                { 0x8d64eb193bc9f334UL, 0xe1ba9f719f08b749UL },  // [42] p c6
                { 0x3d19b49033e1ba76UL, 0x791bd45274ee891cUL },  // [43] p d6
                { 0x31b2cff223bc6dd6UL, 0x8fee5a01e46f6c1cUL },  // [44] p e6
                { 0xd9feacdcdaa0b265UL, 0x77e352a2c9c95e13UL },  // [45] p f6
                { 0xea1d88924be2d33bUL, 0xe4c5daf0f19f2616UL },  // [46] p g6
                { 0x435540503cf78b33UL, 0x15ea306a46b117ecUL },  // [47] p h6
                { 0xae8206a83046ad8aUL, 0x2fa11a29a918cc95UL },  // [48] p a7
                { 0x5997fd3ccc2afe65UL, 0xaa51d7e3c5e73c93UL },  // [49] p b7
                { 0x1773dfba40bd6b97UL, 0xff947d8468f1b8b3UL },  // [50] p c7
                { 0xeec80c0881bf7bb9UL, 0x7f99ee4ef58e862dUL },  // [51] p d7
                { 0x6879e0144024775bUL, 0x8b22d1dc2090e6e0UL },  // [52] p e7
                { 0xf31fe59ebe8fa183UL, 0x1b9da84d03e60cfcUL },  // [53] p f7
                { 0xc30aafe53e3a5b7cUL, 0xfdd7273a8c88b279UL },  // [54] p g7
                { 0xab8118751e84be8bUL, 0x80bc712aab10148fUL },  // [55] p h7
                { 0x716cb7cde057ac9eUL, 0x32f62cd58bc73a35UL },  // [56] (not used)
                { 0x2a8efa8a4f9e5f7aUL, 0x0ecbf2401eeb6e92UL },  // [57] (not used)
                { 0x83164812fb1f9fc1UL, 0x106eeafb908c7ce0UL },  // [58] (not used)
                { 0xbfb8af2cfa2b852bUL, 0xb79f0b090f946bfeUL },  // [59] (not used)
                { 0x5c7af41cad551ab4UL, 0x37f23137789132d7UL },  // [60] (not used)
                { 0x3890136439c74140UL, 0xcdb5959d1d60a129UL },  // [61] (not used)
                { 0x2bf4ad8c1dccd9c0UL, 0x9ecedc72a879ffa4UL },  // [62] (not used)
                { 0x7a7a2d2d46479db7UL, 0x1ccbf1b16c755c39UL },  // [63] (not used)
            },
            {
                { 0xb977cad39627de55UL, 0xf2c61829ace6e61dUL },  // [ 0] n a1
                { 0x887a0d3e8e1ef7e2UL, 0x0b83cb7bb6fe69dfUL },  // [ 1] n b1
                { 0x6bc310da7782f8a0UL, 0x6895b8fa092807baUL },  // [ 2] n c1
                { 0xceae65a14cd55f2aUL, 0x608ca005d0aed2e6UL },  // [ 3] n d1
                { 0x21ab477d2f32733fUL, 0x2f756099bb7079ffUL },  // [ 4] n e1
                { 0x322bd2510c8e75faUL, 0x953ca435f1fa7a38UL },  // [ 5] n f1
                { 0xb7025be075503057UL, 0x4132c6441d034c93UL },  // [ 6] n g1
                { 0xff0c19e9b5a2dfdcUL, 0xec62842d085f40c8UL },  // [ 7] n h1
                { 0x22ff0d8de2f46b9bUL, 0x31d41c283abe23bdUL },  // [ 8] n a2
                { 0xfa5018b1e9123077UL, 0xd206789ebe78ad43UL },  // [ 9] n b2
                { 0x47c6e9b7e72497ecUL, 0xce8715999f47169eUL },  // [10] n c2
                { 0xabcf992ec5125eccUL, 0x90d2792a8bb9ddfcUL },  // [11] n d2
                { 0x227423fe7b128545UL, 0xd20982b7f14efe59UL },  // [12] n e2
                { 0x16e4ce4a7ba7c288UL, 0xa5b5b5f1b9afc2f2UL },  // [13] n f2
                { 0xa3ec8f990cd3c07fUL, 0xd1b1c9df317a3e40UL },  // [14] n g2
                { 0x5fbda8c202a1992bUL, 0x41a2c61f417bb3d2UL },  // [15] n h2
                { 0x0aeb3679cacfdb54UL, 0x75d7c7064fd289e5UL },  // [16] n a3
                { 0x0bf6800749c496c3UL, 0xcb7cc4e783e3ba04UL },  // [17] n b3
                { 0x252b54f801124d1fUL, 0x985d8525e63ef45aUL },  // [18] n c3
                { 0x6ee36e3253c749bbUL, 0x5d5c1be4e5ec97cfUL },  // [19] n d3
                { 0xf56f75ec7b1e2806UL, 0x0f568c7d17c40269UL },  // [20] n e3
                { 0xf694fa9fe833e46eUL, 0x6677b1f18830eba6UL },  // [21] n f3
                { 0x731aa4cbc2c9aa7dUL, 0xc18ad541257b9940UL },  // [22] n g3
                { 0x3b4484f6a971c450UL, 0x5b77b88dd7892a71UL },  // [23] n h3
                { 0xefb730087dbf3c68UL, 0xd1933628733fc8e5UL },  // [24] n a4
                { 0xcc8fe63dcd74adc6UL, 0xed2ff6bdd352da7bUL },  // [25] n b4
                { 0xf5699c94f1615efbUL, 0xe3a735bfd00ed163UL },  // [26] n c4
                { 0x222c29494f48479fUL, 0xdc1c5201a5ff2f59UL },  // [27] n d4
                { 0x947bf359a9d48873UL, 0x2fdd366bfe968035UL },  // [28] n e4
                { 0x4b1f8179d5126775UL, 0x0cbc133dbc2dfda2UL },  // [29] n f4
                { 0x0e2139132300f576UL, 0x366252d41311b191UL },  // [30] n g4
                { 0x3d8b603ea45096eeUL, 0x236170199d68882bUL },  // [31] n h4
                { 0xcadb5d048efe3574UL, 0xbc660dfc3651378fUL },  // [32] n a5
                { 0xc569b1239f0b4304UL, 0x2e41665fb2f73bd7UL },  // [33] n b5
                { 0x20dc89a7266c918fUL, 0x507908dfbc5e8ac3UL },  // [34] n c5
                { 0xce5a1f9e7a9d71d1UL, 0x22d1dec25ff9d99bUL },  // [35] n d5
                { 0xd0ed6e3fec497b5aUL, 0xd42e5d8baaeb0b32UL },  // [36] n e5
                { 0x564a7f238ba02c31UL, 0x8316e7f71578e19cUL },  // [37] n f5
                { 0x5f0f0f55cc1d7c2cUL, 0x7a55c6ccdcf6677aUL },  // [38] n g5
                { 0x5ef2d0e8bd617934UL, 0xa49bf086d89d8617UL },  // [39] n h5
                { 0x65b40c8b81843290UL, 0x9315361be8a5a003UL },  // [40] n a6
                { 0x6adb2341b12ef5faUL, 0xd4a9dbb8e0d4b0a1UL },  // [41] n b6
                { 0x5c5b44578980422eUL, 0x1106282f04dfc3e1UL },  // [42] n c6
                { 0xf4166ac77be07b51UL, 0x2c1275fb6379508eUL },  // [43] n d6
                { 0xfc2ba6007cb15667UL, 0x116245520d436718UL },  // [44] n e6
                { 0xc74b8a6cf19ea337UL, 0x76fa9bebdd617b91UL },  // [45] n f6
                { 0xf69e4bc3ba4ba9d6UL, 0x9953c2ea4de603b3UL },  // [46] n g6
                { 0x0924b914ce4390ebUL, 0x24e207616d7c6357UL },  // [47] n h6
                { 0x435ce0c73dbe1c45UL, 0x0e40f01a4d493393UL },  // [48] n a7
                { 0x64c3d6ba273fb76eUL, 0x68af493dbeb98267UL },  // [49] n b7
                { 0x60724183607deea9UL, 0x0555a8de750d485dUL },  // [50] n c7
                { 0xa7c588deeddefc0eUL, 0xfb6ec9c34f2513d4UL },  // [51] n d7
                { 0xbb6862adf940f269UL, 0xb069ac5b0adf3096UL },  // [52] n e7
                { 0x399f6e13a2cd7af3UL, 0x9051aa5ade22d071UL },  // [53] n f7
                { 0x79dfcd56ea273c3dUL, 0xf9218921116acfb9UL },  // [54] n g7
                { 0x1f04ca4dda0c739eUL, 0x73e0c3554a9bdcf4UL },  // [55] n h7
                { 0xe0477888603766fdUL, 0xec4bbaf88df1ef68UL },  // [56] n a8
                { 0x999d99642463cecaUL, 0xb2ddbecc09cbbf87UL },  // [57] n b8
                { 0xf0e4e55c2cd7b5faUL, 0x083a6fbf13f173b7UL },  // [58] n c8
                { 0xa1b97fb1135282abUL, 0x12221e121639c763UL },  // [59] n d8
                { 0xb1b3c1ec5f0b77eaUL, 0xb3bbed5d724e7931UL },  // [60] n e8
                { 0xf8f1f1b4b9d9bef3UL, 0x53314061a4231a2dUL },  // [61] n f8
                { 0x0d52efe5af93d65fUL, 0x8032e9c106c2ecb0UL },  // [62] n g8
                { 0x3dad559aa93d2aeaUL, 0xa61f733cda9a7326UL },  // [63] n h8
            },
            {
                { 0x3054f108d35a40fbUL, 0x97260900d7e30f29UL },  // [ 0] b a1
                { 0xd4ab1f294fe0607bUL, 0x29fa74cd0574701aUL },  // [ 1] b b1
                { 0x56a61de60744a2beUL, 0x4852b3c0980684a5UL },  // [ 2] b c1
                { 0x411a4201e25127aeUL, 0x46d2a20fcf2bde16UL },  // [ 3] b d1
                { 0x0657247add32a17dUL, 0xb6bccd379f3e8a7bUL },  // [ 4] b e1
                { 0xb9f32e256bb610eaUL, 0x367103ee822df182UL },  // [ 5] b f1
                { 0x83dbc2255dbfea27UL, 0x626b10ca064f4dfeUL },  // [ 6] b g1
                { 0x794d7973b01707c1UL, 0xc0c6bd65724fbb46UL },  // [ 7] b h1
                { 0xb57e147fbd609c2bUL, 0x0296e99b9056d8d1UL },  // [ 8] b a2
                { 0xe95916a47ff0dfe4UL, 0x1e47163370f2e953UL },  // [ 9] b b2
                { 0x99beb6f7f9827856UL, 0x9df9db61aec892c7UL },  // [10] b c2
                { 0xa57d917a233795a5UL, 0x2532643ed94d453bUL },  // [11] b d2
                { 0xc5a6357cd3826bbcUL, 0x59687a83aeef6169UL },  // [12] b e2
                { 0xc3948cd31bdb14c1UL, 0xbf095e79453134e8UL },  // [13] b f2
                { 0x6f8eeec49df72046UL, 0xe46bf3fa32e9cd3aUL },  // [14] b g2
                { 0x5e757f787865ccb6UL, 0x0792dbf3c209387dUL },  // [15] b h2
                { 0x92542b9621ecbf9aUL, 0xf7caab8f33922ac2UL },  // [16] b a3
                { 0x22e134936986df96UL, 0x3798e5316971eee2UL },  // [17] b b3
                { 0x542739f729a90585UL, 0x14cf24ce88565e80UL },  // [18] b c3
                { 0xbd8e65cd6c4c85a6UL, 0x40aaa9a225de056aUL },  // [19] b d3
                { 0xa4712ad075b56048UL, 0x03d666d9ff5b5d46UL },  // [20] b e3
                { 0xded25be7bc0482afUL, 0x85b282883bb48243UL },  // [21] b f3
                { 0x7f70c05a49f57f05UL, 0x8bcb45afc51ae571UL },  // [22] b g3
                { 0xd72154c8d95c87bbUL, 0x50303d5f70588531UL },  // [23] b h3
                { 0x1c17b98153f19c62UL, 0xf2bd066340d251eeUL },  // [24] b a4
                { 0xbb2cf69ab58fac0bUL, 0xd5172e9ba94d0065UL },  // [25] b b4
                { 0x392b286cad54f13aUL, 0x2dba07cb5a7531bcUL },  // [26] b c4
                { 0x8d80b1b75c69b095UL, 0xd11464eb3842a2deUL },  // [27] b d4
                { 0x2a56e582a32cfc98UL, 0x52c52378133f4441UL },  // [28] b e4
                { 0x80a9353e978964e5UL, 0x77681e684c0f5c3eUL },  // [29] b f4
                { 0xa680f2afcf195647UL, 0x6e8de97a4d8f7ea1UL },  // [30] b g4
                { 0x7bdbd4fe206ddc17UL, 0x8fe3c2cb54b03374UL },  // [31] b h4
                { 0xb5bff50f275f09dcUL, 0xba9ce08b3be4a554UL },  // [32] b a5
                { 0xfcb23812d9887b67UL, 0x0914631f071fc2c0UL },  // [33] b b5
                { 0x9115230d6b80c569UL, 0x3eb6e1c219139da3UL },  // [34] b c5
                { 0xd313c495b0455975UL, 0x1779ae29a497a3bdUL },  // [35] b d5
                { 0x54490daec441b691UL, 0x42665ea609ac3e8dUL },  // [36] b e5
                { 0xd7191734b6d250c9UL, 0xb6d66f43b4c6c44dUL },  // [37] b f5
                { 0x38a0b68aea1934b6UL, 0xb1b3f4fdc9604c80UL },  // [38] b g5
                { 0x1d7f860b076683f0UL, 0x3e0811a24ef214a1UL },  // [39] b h5
                { 0xaa3e7919abe58804UL, 0xb539561f39495585UL },  // [40] b a6
                { 0xd800b13f0f765b1bUL, 0xfd0296b7d5a09877UL },  // [41] b b6
                { 0xf3b2d51a5c304edeUL, 0x7b026864069b8fecUL },  // [42] b c6
                { 0xc5634f90fcf5992dUL, 0x8991d25a2106fc5dUL },  // [43] b d6
                { 0xe3a2981fa750c0b6UL, 0x4d8309a85386417fUL },  // [44] b e6
                { 0xc25a20da71fa9a1bUL, 0xfe1f8e041869abd5UL },  // [45] b f6
                { 0x3fbdf583dce69b7dUL, 0xa99e85a22872f727UL },  // [46] b g6
                { 0x18637cd504649312UL, 0xfc4dfcf5425d1736UL },  // [47] b h6
                { 0xb17aa50198bd882aUL, 0x9cfb664d18c76f5bUL },  // [48] b a7
                { 0x8d0a9ba94ed444d3UL, 0x25fcd561229de3b5UL },  // [49] b b7
                { 0x34ef5634e1ae0523UL, 0x2de9723e9c74e302UL },  // [50] b c7
                { 0x3390a00ded36dde4UL, 0xd64899a8f4aa95bcUL },  // [51] b d7
                { 0xc8f31276621ed2baUL, 0x5f0cbbc49fa61d20UL },  // [52] b e7
                { 0x035115a4054a72b1UL, 0x555b4ea578ee7c3fUL },  // [53] b f7
                { 0x12cdf7ff208baeadUL, 0xb567928fbd74c864UL },  // [54] b g7
                { 0x3334f804929a1fb2UL, 0x664032c1c01c3dc1UL },  // [55] b h7
                { 0x352c569bc748ab8fUL, 0x11837f19ef03b76cUL },  // [56] b a8
                { 0x788994f2c2c00561UL, 0x202505a1da85ff80UL },  // [57] b b8
                { 0x883702e0b72b9404UL, 0xce4104e1c6e3b6d1UL },  // [58] b c8
                { 0x155e130b80e2e9feUL, 0xf420152efe7da69fUL },  // [59] b d8
                { 0x837727a3f2f1eba2UL, 0x2e2414b419ae6446UL },  // [60] b e8
                { 0xe4eeb171076a5fc0UL, 0xaade866673646e8cUL },  // [61] b f8
                { 0xcb300f97bd428531UL, 0xbe4d1abcd22fd174UL },  // [62] b g8
                { 0x13a9955cf741e469UL, 0x42564e98da52ab71UL },  // [63] b h8
            },
            {
                { 0x3d99c53ccef29943UL, 0x3b39cb14d476dc2eUL },  // [ 0] r a1
                { 0xb2b3905bc07d493aUL, 0x94750ed06923b8e3UL },  // [ 1] r b1
                { 0x9bb66f9e11c18f9eUL, 0x5999b48a4e070e3aUL },  // [ 2] r c1
                { 0x506c309a67aa17ccUL, 0xf6eed3dcfb66aa94UL },  // [ 3] r d1
                { 0x8675042a8b5490daUL, 0x4b02665854b06a0bUL },  // [ 4] r e1
                { 0x4337a172da9d0ae7UL, 0xca1350e7d5f9c2bbUL },  // [ 5] r f1
                { 0x7d9754545c9007c8UL, 0x6e78486906fdeaa8UL },  // [ 6] r g1
                { 0x3b4164a994c50e45UL, 0xe97ab76ac1d4a0c8UL },  // [ 7] r h1
                { 0xd02ee78f6111032fUL, 0x61065de0067f615fUL },  // [ 8] r a2
                { 0x9a6668c15824d62eUL, 0xc5a2047be10f3d06UL },  // [ 9] r b2
                { 0x56207a511dc1d49fUL, 0x68c4352d61e63436UL },  // [10] r c2
                { 0x35c371352afb5e48UL, 0x30db294de30839ffUL },  // [11] r d2
                { 0xc82bb3e3423de3f4UL, 0x332418c8f981eda9UL },  // [12] r e2
                { 0x81c00e4b3f3d414cUL, 0x2b2b9cf819b1c4d0UL },  // [13] r f2
                { 0xb4e2194a8ef9438bUL, 0xfe487b76125ff195UL },  // [14] r g2
                { 0xee346fc0f0c5351fUL, 0x765692b0496e8b04UL },  // [15] r h2
                { 0xa1438d713443b8bbUL, 0x07806bfa735fbe3dUL },  // [16] r a3
                { 0xd3465a5b58761f82UL, 0x79e89c06ce96fd17UL },  // [17] r b3
                { 0xc2ecc7160297a217UL, 0xc41c04090b4b805dUL },  // [18] r c3
                { 0x5396022c9e4c12e2UL, 0x4fcaea5431e9d026UL },  // [19] r d3
                { 0xdcc9ac42822d686bUL, 0x846c890cbf15ac54UL },  // [20] r e3
                { 0x5949d86d90e75e91UL, 0x26844a42fef9c54dUL },  // [21] r f3
                { 0x794d9257e416c1f9UL, 0x79bfbd2f7d4163c7UL },  // [22] r g3
                { 0x5c3ca9cfe7a42e3dUL, 0xcc0ecd6fbbf5f262UL },  // [23] r h3
                { 0x9599bb438cf77417UL, 0x64e91fb7e370ab39UL },  // [24] r a4
                { 0x3ed4733b83bdb5fbUL, 0x09c9d3b22c09af67UL },  // [25] r b4
                { 0xadd09058e6a36ce6UL, 0xbffc9f6232f5c072UL },  // [26] r c4
                { 0x36cb2a27b973c95dUL, 0x0b6f8cc1e95bffa1UL },  // [27] r d4
                { 0x43219c1359a3a3b3UL, 0xc4806d05dcf103e6UL },  // [28] r e4
                { 0x197fff67d9ece08bUL, 0xdb90f4865381ab58UL },  // [29] r f4
                { 0x3dba12fe2780257aUL, 0x6d6643b6e2c1efe8UL },  // [30] r g4
                { 0xdd1fce95a0fe3d3cUL, 0x81a960f122c68e8bUL },  // [31] r h4
                { 0xd586f9c433d2935cUL, 0x2fc9140a68a0336dUL },  // [32] r a5
                { 0x8e7468e47908a659UL, 0x42418ddf6e7f90ccUL },  // [33] r b5
                { 0xfa88b53dbdabd4a7UL, 0xebb24f87c1145af3UL },  // [34] r c5
                { 0xe9a15c1a189b2f91UL, 0x529e343d37525a52UL },  // [35] r d5
                { 0xc51d6f36a41614e5UL, 0x8dfdc83811c93d35UL },  // [36] r e5
                { 0xb1fae8eba1ba0ccfUL, 0x3856e9118a17685aUL },  // [37] r f5
                { 0x0eb476c0b04bdf8eUL, 0x5e6b505a95215c3fUL },  // [38] r g5
                { 0xa6e8e12c8dfcb736UL, 0xa12162d79c180506UL },  // [39] r h5
                { 0x0ec8d9b95f3b5294UL, 0x781967e40eb82b1eUL },  // [40] r a6
                { 0x0ba36426a52c90d9UL, 0x2a09ab62af14ca6eUL },  // [41] r b6
                { 0xbedfe4f1463a48acUL, 0x86f1a7ae33213163UL },  // [42] r c6
                { 0x08e99f0d6d1b5f50UL, 0xca6791a926a9bbb3UL },  // [43] r d6
                { 0x8aa4813d32829b26UL, 0xf6a47f4deb212cdbUL },  // [44] r e6
                { 0x1c301579b5b978a3UL, 0x33f8735758b76654UL },  // [45] r f6
                { 0x24e79f39af288103UL, 0xb75e9f33b93cd895UL },  // [46] r g6
                { 0x611a7f3e4f18f586UL, 0x4a42aa756214f0feUL },  // [47] r h6
                { 0xd22673791a63b9c4UL, 0x3c81cb0c5f02b51eUL },  // [48] r a7
                { 0x4d3d1c09a871d74dUL, 0x57b533551b5d1948UL },  // [49] r b7
                { 0xda72370f2a013cd7UL, 0xfccf8a82c93ccacdUL },  // [50] r c7
                { 0xec98e155234b49bfUL, 0xca3624677f00a40fUL },  // [51] r d7
                { 0x4b79f6d507432f40UL, 0xa58318050aca7173UL },  // [52] r e7
                { 0x65945e038d1334beUL, 0x7325e9c69de173daUL },  // [53] r f7
                { 0xf07d6afa39d1ad97UL, 0x62696c9da4683405UL },  // [54] r g7
                { 0x326ef8b1e46e36a9UL, 0x85ecda8ac1012a32UL },  // [55] r h7
                { 0xc52e03cc981542d5UL, 0x3e8b9674a88bc93fUL },  // [56] r a8
                { 0xa62b9fcdc6c54a0aUL, 0x0e2292a5021a6b95UL },  // [57] r b8
                { 0x5530a54af660edfcUL, 0x441c836d891793d0UL },  // [58] r c8
                { 0x8549d3db786fbfb3UL, 0x542f07dd6f89c5faUL },  // [59] r d8
                { 0x6ca4da12f5586c29UL, 0x85c55d7be015a8bfUL },  // [60] r e8
                { 0x1a032a65683b51f9UL, 0x7b396d49971eab82UL },  // [61] r f8
                { 0xa7e4d427004362c7UL, 0x0f1c551f4e33f3cfUL },  // [62] r g8
                { 0xc78cf1d28a9ea982UL, 0x51b858ec9528f172UL },  // [63] r h8
            },
            {
                { 0x98c5985625313947UL, 0x11961d61ff0a21b7UL },  // [ 0] q a1
                { 0x9e4687f215357152UL, 0x7d4eb6e5c4e6a3ffUL },  // [ 1] q b1
                { 0x698273b4cc7a03aaUL, 0x8d6365b06c848de2UL },  // [ 2] q c1
                { 0x71aa7e1a684d43ecUL, 0x5e6d802eb878b663UL },  // [ 3] q d1
                { 0x1d6667449a0e3c1fUL, 0xce93cdefab614b5dUL },  // [ 4] q e1
                { 0x793195822d684c85UL, 0x1d0956596e78e2c5UL },  // [ 5] q f1
                { 0x80306429aa3c1eecUL, 0xcbeede37b339f9b6UL },  // [ 6] q g1
                { 0xd734cb4f757238f0UL, 0xc0766a69ab395449UL },  // [ 7] q h1
                { 0xc57d8afc82868c74UL, 0x27153f1d0851d344UL },  // [ 8] q a2
                { 0x1efa9e47d7f3c284UL, 0x5e6f7fe8264d0fc6UL },  // [ 9] q b2
                { 0x46b002289421cde0UL, 0x50c78e6bd6f57c50UL },  // [10] q c2
                { 0x7f5922558e99088cUL, 0x5edeb067dbb8d434UL },  // [11] q d2
                { 0x207e5b5eb32a1585UL, 0x7cc8bd1e0dda5e97UL },  // [12] q e2
                { 0x7dcc93b0363e5ea2UL, 0xb4c22ca1931d6762UL },  // [13] q f2
                { 0x2143884070e2f5caUL, 0xacb846acb6ba45cbUL },  // [14] q g2
                { 0x81d340d0e28bae4eUL, 0x68c2a135413b93e6UL },  // [15] q h2
                { 0xfc7506131dcd6226UL, 0x023a5791731a2f2eUL },  // [16] q a3
                { 0x301ec6009372dd65UL, 0x1528d7c3f5cfe12cUL },  // [17] q b3
                { 0x58d14fcd2efbd5acUL, 0xc119484a323e4d24UL },  // [18] q c3
                { 0xdb41518c1366b363UL, 0x0d2f6d91f3aed918UL },  // [19] q d3
                { 0xb082627d77404cb6UL, 0x21e16be6241fb574UL },  // [20] q e3
                { 0x63de68668d794999UL, 0x8f12c86528b8cfbeUL },  // [21] q f3
                { 0xafbc6a2255422a03UL, 0x67e39fc98e6273a1UL },  // [22] q g3
                { 0xd574b36c0ccb210dUL, 0xd68d8faa56b29ea3UL },  // [23] q h3
                { 0xd1347dc801f080e7UL, 0x672cb1264682c5a6UL },  // [24] q a4
                { 0x01bade792aa820a8UL, 0x0065a61c0293fbb3UL },  // [25] q b4
                { 0x068c16b642cac005UL, 0x567af1757b16ec58UL },  // [26] q c4
                { 0x70b151bc2af2a268UL, 0xf5bdcf18e595cfb4UL },  // [27] q d4
                { 0x651a00dd3ab86aadUL, 0x58961a221d786b34UL },  // [28] q e4
                { 0x14719fb445a8348eUL, 0xee019a236665096fUL },  // [29] q f4
                { 0xceac1deeca85c5eaUL, 0x5fb487dcfd621603UL },  // [30] q g4
                { 0x2e7e29210ae150f8UL, 0x9d91c2ae31cacc72UL },  // [31] q h4
                { 0xa49ceccef0c9dfc3UL, 0xe0a20ec0da308369UL },  // [32] q a5
                { 0xf0a4815c085357e8UL, 0xfc26fedaf44ff451UL },  // [33] q b5
                { 0xa6ee957c062e9f9eUL, 0xd445f91502605d85UL },  // [34] q c5
                { 0xe0e5164c8cb47431UL, 0x294598b875329a9cUL },  // [35] q d5
                { 0x3f784f0950e010b7UL, 0x229027b0ca121a10UL },  // [36] q e5
                { 0x830a7f6fedb64a5bUL, 0xb52a01609af442c0UL },  // [37] q f5
                { 0xdb828feaeb0f8019UL, 0x7d5944d1492b1abbUL },  // [38] q g5
                { 0x18d3e01f7ce3fcbeUL, 0x59ad8f27cd06d295UL },  // [39] q h5
                { 0xa413c8e9aafea60cUL, 0x3785fd04b5f66ba8UL },  // [40] q a6
                { 0x9b89169acb0a76d0UL, 0x94a2d3c4fc6fcaaeUL },  // [41] q b6
                { 0xd5e4f3b00d1d43c3UL, 0xf97e6b11fb0a2929UL },  // [42] q c6
                { 0x6b9caabe7f00a697UL, 0x10719401b25ccc76UL },  // [43] q d6
                { 0x42355e4f8fb964aeUL, 0x066fa7cb85966dc0UL },  // [44] q e6
                { 0x7221505f2f7bb159UL, 0x2327ffd9041ac240UL },  // [45] q f6
                { 0xb3aaf6a1b0385e34UL, 0xc05f5a90ce34d589UL },  // [46] q g6
                { 0x2f18171323ea4bacUL, 0x9d31acb4c3e3970fUL },  // [47] q h6
                { 0x0b2f6b88287d8c4dUL, 0x151f0f658e46012bUL },  // [48] q a7
                { 0x015637dec1d3d42fUL, 0xa7940dda304573d4UL },  // [49] q b7
                { 0xd0f465e61f86b35eUL, 0x70a48c7d1d1e12faUL },  // [50] q c7
                { 0xb08704d01682289dUL, 0x43930f47493ff4b7UL },  // [51] q d7
                { 0x56275ff579165a24UL, 0x802df369aca527dcUL },  // [52] q e7
                { 0x809ddaeecbb84406UL, 0x7b8ffe349f6129c3UL },  // [53] q f7
                { 0xc5df7ee4c85c31deUL, 0xda5f07d3af26ce65UL },  // [54] q g7
                { 0x99c2d0a89337b183UL, 0x1a59fb8241ec6dabUL },  // [55] q h7
                { 0xd51eab5ef8ce7c97UL, 0x9d725b3126f51f2fUL },  // [56] q a8
                { 0x962526b0e08dc2faUL, 0xc431d74971ae5991UL },  // [57] q b8
                { 0x3af375a0a06ccf62UL, 0xfbe3e950957c6329UL },  // [58] q c8
                { 0xf6085968f1203ed3UL, 0xb50bf74721baa8d6UL },  // [59] q d8
                { 0x53fbfd224b234010UL, 0x635eaebdfb714795UL },  // [60] q e8
                { 0xe4d8aab05e910726UL, 0xe66da0d9b40ec3c9UL },  // [61] q f8
                { 0x0292c5194f3711c2UL, 0x174f976ae2a12cb1UL },  // [62] q g8
                { 0x20c69ed633873c3cUL, 0x94f204c32e130159UL },  // [63] q h8
            },
            {
                { 0x300172277f3e3489UL, 0x41aea11eb5b273abUL },  // [ 0] k a1
                { 0x7a3d2bdcf677c56fUL, 0x7acaeadf69c6d96bUL },  // [ 1] k b1
                { 0xeeb9c25704878632UL, 0xd1953334d21ab672UL },  // [ 2] k c1
                { 0x44ba1566ef9d1e92UL, 0x42c0fa46ebfd8d97UL },  // [ 3] k d1
                { 0xc6b83a8321334438UL, 0x50a1dbc85366323dUL },  // [ 4] k e1
                { 0x6669b505859bd3c2UL, 0xdd5abf536b0ccb0aUL },  // [ 5] k f1
                { 0x6d2f2cf88412fb24UL, 0x51964049471827c1UL },  // [ 6] k g1
                { 0xb4f0391f29ca69d5UL, 0x9f310e7cd4b76c9aUL },  // [ 7] k h1
                { 0x2aa482b4b5477618UL, 0xbb5f4933df00001fUL },  // [ 8] k a2
                { 0x7c5f90c4b8d2d7d3UL, 0x3d9c8e0d2a22caeaUL },  // [ 9] k b2
                { 0xe1149f75ce73f384UL, 0x1e4fe9f6c0b7bd10UL },  // [10] k c2
                { 0x493fe53c8ee14d0cUL, 0x8322666e58dce5e8UL },  // [11] k d2
                { 0x0cf465b254eab7b9UL, 0xc7841f907d377e6eUL },  // [12] k e2
                { 0x65ed4a0933535323UL, 0x91bae032fbf6ac76UL },  // [13] k f2
                { 0xef17ce870d45a951UL, 0xdf6f99d73ea56b85UL },  // [14] k g2
                { 0x672436574b7a6751UL, 0x131a0666cf7cc6ecUL },  // [15] k h2
                { 0x80e651f844745fd5UL, 0x3ef2ada51b51f08eUL },  // [16] k a3
                { 0x57872e257b20568aUL, 0x5002ba822dbfd6e8UL },  // [17] k b3
                { 0x92d92fc9b42abeceUL, 0xce118f2e01e5a5deUL },  // [18] k c3
                { 0x5ea70470507a8142UL, 0x2c08c4fc74202adaUL },  // [19] k d3
                { 0xc09c95e68baa0a38UL, 0xd34f192c9e4ec235UL },  // [20] k e3
                { 0x7e731804d8381983UL, 0xf58a248f5c2ac624UL },  // [21] k f3
                { 0x3ed3aec64612fbd5UL, 0x2d8016e9be2ea5b7UL },  // [22] k g3
                { 0xe92df3f9d78b3195UL, 0xf5de47dd1c938343UL },  // [23] k h3
                { 0x68058c28561b9092UL, 0xe36548f1c3005b85UL },  // [24] k a4
                { 0x0f08d00d7d9ad6f4UL, 0x9ab7388f05752268UL },  // [25] k b4
                { 0x712b1e1c17d03ffcUL, 0xcb943bf7b66161edUL },  // [26] k c4
                { 0x895be46cf2fd2630UL, 0x8748d55a164a7077UL },  // [27] k d4
                { 0xd30fc927efebe517UL, 0xef4561d7e8408a43UL },  // [28] k e4
                { 0xf599a6805204c1d0UL, 0xc880f9c83080748dUL },  // [29] k f4
                { 0x49ab44d380c49ebeUL, 0x2f3df98587b4101aUL },  // [30] k g4
                { 0xca6e8316b080cc9eUL, 0x6324937f495af4d1UL },  // [31] k h4
                { 0x4f3ba2889961718aUL, 0x4adfce0161e08c4fUL },  // [32] k a5
                { 0x5a5e42715945b358UL, 0xc0311a088eaa207bUL },  // [33] k b5
                { 0x8487c2a1873b150fUL, 0x1d6633ba18cf3f02UL },  // [34] k c5
                { 0x862ec203200ed64eUL, 0x3cca92d9dbf1793cUL },  // [35] k d5
                { 0x02e4bdfd7f8a8f24UL, 0xccde1234d01699faUL },  // [36] k e5
                { 0x04c7242e720204d8UL, 0x692a061497e42ba9UL },  // [37] k f5
                { 0xc0a7ce6a7c0d8e3cUL, 0xe35c16484557320aUL },  // [38] k g5
                { 0x5e70d81b979161a2UL, 0xb235c1313a8f5a1bUL },  // [39] k h5
                { 0x1f0220e92642b7d1UL, 0xd114dc414bb41a39UL },  // [40] k a6
                { 0x4c6ed84454dfd360UL, 0x47db3a47b48b58b7UL },  // [41] k b6
                { 0x35820091f5f72109UL, 0x0143c71c789e442eUL },  // [42] k c6
                { 0xaf1830a5529940a2UL, 0x4fcbf0c265843513UL },  // [43] k d6
                { 0xd68515791df6f49fUL, 0xd827ca25f982c2dbUL },  // [44] k e6
                { 0x081e1b548ed3d7cbUL, 0x73e3311f3bdede2aUL },  // [45] k f6
                { 0x5d3bd78b4f4c49fdUL, 0x6e4a102b944ae3cdUL },  // [46] k g6
                { 0x2224f7e7717f8f01UL, 0x9b9bab8d1abadffcUL },  // [47] k h6
                { 0x1d4479867bcd832eUL, 0x0940729dca3cb14aUL },  // [48] k a7
                { 0xdae58677077241e9UL, 0x4587d4eb923b2816UL },  // [49] k b7
                { 0xfcd664f2323235e8UL, 0x16280bc9c9ea743fUL },  // [50] k c7
                { 0xfe18be3e8bb8d322UL, 0xe8e9d4b1c7b54f1aUL },  // [51] k d7
                { 0x74297efd26f285c6UL, 0xf56d507265aafb2eUL },  // [52] k e7
                { 0xa7952472374cc060UL, 0xcf98ead0d62100b9UL },  // [53] k f7
                { 0x3b31007b171212f7UL, 0x991e7798fae416faUL },  // [54] k g7
                { 0xe88db257ff1c7bd2UL, 0xfd5ba73f6437bdcbUL },  // [55] k h7
                { 0xf4e83d1beae570bfUL, 0xe00da6f760394b92UL },  // [56] k a8
                { 0xbd4d72e62869e89aUL, 0xa5593b4e23854a31UL },  // [57] k b8
                { 0xf1e5b4565e493ec0UL, 0x4406da99e1c7b23dUL },  // [58] k c8
                { 0x2ce0994f47fa96ecUL, 0x4bed768781cc1fd1UL },  // [59] k d8
                { 0x967815ae0374b802UL, 0x38077b931f9fe10dUL },  // [60] k e8
                { 0xdf5f5515d0f75b28UL, 0x0b6aa59cf107d5d3UL },  // [61] k f8
                { 0x88b57b702cbf9258UL, 0x66a9c7749bf7a15cUL },  // [62] k g8
                { 0x1e36fdbc7c32abe0UL, 0x56fb7ae55c2387a9UL },  // [63] k h8
            },
        };
    }
}

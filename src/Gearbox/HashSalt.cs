namespace Gearbox
{
    internal static class HashSalt
    {
        internal static readonly ulong[,,] Data = new ulong[12, 64, 2]
        {
            {
                { 0x3946a67a20982243UL, 0x5c7efdcf07619f1bUL },  // [ 0] White to move
                { 0x0ee565522c796a42UL, 0x8adc48d58f70a01bUL },  // [ 1] White can O-O
                { 0x6c90be5133ef0036UL, 0xf35d1109f23bfcd7UL },  // [ 2] White can O-O-O
                { 0x051c7046123d468eUL, 0x69c523ab34d21541UL },  // [ 3] Black can O-O
                { 0x6e8199a1d5b540b4UL, 0x1713b4d3a7d5afceUL },  // [ 4] Black can O-O-O
                { 0xff83172acf541928UL, 0x1304dd9a6e75b277UL },  // [ 5] (not used)
                { 0xe4e0b0141da48310UL, 0xd718f1c648afca8aUL },  // [ 6] (not used)
                { 0x3dd979c7f194767bUL, 0xfef42a59d060ab53UL },  // [ 7] (not used)
                { 0xb2ab4f799b374092UL, 0xd3be5bf21fb4b40eUL },  // [ 8] P a2
                { 0x317f02ad2eb6dca8UL, 0xe6d092924ec2eca0UL },  // [ 9] P b2
                { 0x17f0883e0f5f30e3UL, 0xd5f851b85f9c10ddUL },  // [10] P c2
                { 0x82efd4c85cfd216aUL, 0x6cae335607fbad42UL },  // [11] P d2
                { 0xf9b98a27d21a031fUL, 0x62b8938dc5c9bb4cUL },  // [12] P e2
                { 0xdd723fe294b73d49UL, 0x8cf4d25bb7f750eeUL },  // [13] P f2
                { 0x389cc43ee4017effUL, 0xac3bead327b7e0d4UL },  // [14] P g2
                { 0x709523659680e381UL, 0x517a82e105d699a4UL },  // [15] P h2
                { 0xd067e7e34509d633UL, 0xe899d13b57aab582UL },  // [16] P a3
                { 0x6a5f5bf4cb78bc5dUL, 0x6433746c903781abUL },  // [17] P b3
                { 0x200242254d7dce68UL, 0x89b53ae073c872ddUL },  // [18] P c3
                { 0x6ec21c7a9f56f056UL, 0x793c9cb4827caf03UL },  // [19] P d3
                { 0xa6ca9ac2c2b40242UL, 0x71ece8933c71d5ccUL },  // [20] P e3
                { 0x8e5165056c93815bUL, 0x288a5e9e9612922dUL },  // [21] P f3
                { 0xc291e9859c8b02d8UL, 0x8514fa8032724828UL },  // [22] P g3
                { 0xbf5a1c82883617f2UL, 0xa3c0add17fb6601fUL },  // [23] P h3
                { 0x7975a2bebc1b8e06UL, 0x5b3ee72836d9d0feUL },  // [24] P a4
                { 0xb6283d8b3e81b6eeUL, 0x49a440080f60ac6fUL },  // [25] P b4
                { 0xa2ca14e693e0029bUL, 0x61a48b2cf322f772UL },  // [26] P c4
                { 0xe4ae88a19bd47847UL, 0xffa2c434fedc517eUL },  // [27] P d4
                { 0xb14a5885b4dc66b4UL, 0x6bd6094e597b6e9fUL },  // [28] P e4
                { 0xdb434882546683e9UL, 0xdb69839103a0737aUL },  // [29] P f4
                { 0xda8b174b7a386a11UL, 0x85f82464f72e906dUL },  // [30] P g4
                { 0xee717e14333765c1UL, 0x0f2f2a3cd0c49900UL },  // [31] P h4
                { 0xbe29dafd0c89f331UL, 0x6845777c66f660fbUL },  // [32] P a5
                { 0xeb13ff87e392dc29UL, 0x4898a81fac56742eUL },  // [33] P b5
                { 0x07c16bc5e3c27903UL, 0x8e06810a92414245UL },  // [34] P c5
                { 0x0bdc773ca9f1383bUL, 0xea37376b9c62d428UL },  // [35] P d5
                { 0x58dd6fba86fd97b3UL, 0x31030af5a3edceadUL },  // [36] P e5
                { 0x5b33b529f1799816UL, 0x37762f0f76f368beUL },  // [37] P f5
                { 0xa02b713da40e5777UL, 0xa885777a858f9771UL },  // [38] P g5
                { 0x2e4c25e29a4e51edUL, 0x513c8cd745cd4599UL },  // [39] P h5
                { 0x8b9dc0dde49c1994UL, 0x8d6335ce96a087eaUL },  // [40] P a6
                { 0x900f50b46d4a1a8eUL, 0xf0ca311ec0b1b3b7UL },  // [41] P b6
                { 0xd87ed4b04722cf24UL, 0xab0c8d153452ab7aUL },  // [42] P c6
                { 0x55359757c05a93e1UL, 0xe1a6c2b5e4c2b84eUL },  // [43] P d6
                { 0x57b8836a800a7ed3UL, 0x3ade66bee1121b6eUL },  // [44] P e6
                { 0x111614f2748e3406UL, 0xc614d065e19de4b6UL },  // [45] P f6
                { 0x7dde1a3a1bb93bc1UL, 0xfa3451061bc83d2aUL },  // [46] P g6
                { 0x3dd04ce5b30f4284UL, 0xb8e632ec92322fbaUL },  // [47] P h6
                { 0xcd1a410cffc6bf99UL, 0x27178ddec153b70cUL },  // [48] P a7
                { 0x941c20be66a8ff45UL, 0xa6e8b19b5d8214d1UL },  // [49] P b7
                { 0x22529fc2f6acd712UL, 0x6d290e0c952f33d5UL },  // [50] P c7
                { 0xb08ab7da2ecd35caUL, 0x652af6d682565463UL },  // [51] P d7
                { 0xf2d7e3c4fcfaee18UL, 0x891be2100085b97dUL },  // [52] P e7
                { 0x591fa9e36064feefUL, 0x42ec9f82943eb816UL },  // [53] P f7
                { 0x7bc2bd8482f9281aUL, 0xd35ebf020bae3401UL },  // [54] P g7
                { 0x0fc3f919d5cbc7f4UL, 0x335d49fa76f416feUL },  // [55] P h7
                { 0xd18b8b60bd05f127UL, 0x4d16f85cc76f8f4eUL },  // [56] en passant target on a-file
                { 0x20e021fde0d7b4a5UL, 0xd227bfa81a892062UL },  // [57] en passant target on b-file
                { 0x0a2dd0d27c274b01UL, 0xb607bce10b3a7ecfUL },  // [58] en passant target on c-file
                { 0x7fc34e685db9b3f8UL, 0x66fce7f840a6ce40UL },  // [59] en passant target on d-file
                { 0x1c5e1267aa018630UL, 0x692e8aa67cd56a43UL },  // [60] en passant target on e-file
                { 0x0fbdf1dd222a3452UL, 0x1cd00995c012e83fUL },  // [61] en passant target on f-file
                { 0x2e4ac8a67ad4afb9UL, 0x88ebfba6cf1dd86aUL },  // [62] en passant target on g-file
                { 0x2ebcc2f4cf53ebaeUL, 0x6ef548d77b7754eeUL },  // [63] en passant target on h-file
            },
            {
                { 0x4fc23cb76b1b90f1UL, 0x9ce7f07b9ff47076UL },  // [ 0] N a1
                { 0xa39506c785647b0dUL, 0xa4571f82381c8b5aUL },  // [ 1] N b1
                { 0x3b33e712fe1d6185UL, 0x0129d5f95180c2a2UL },  // [ 2] N c1
                { 0x950bbbfa8683593bUL, 0x7ef2434ce730b2caUL },  // [ 3] N d1
                { 0xbbd5a43d81dd1690UL, 0xb0ba09f78605db9fUL },  // [ 4] N e1
                { 0x5e7d3b771ccc6368UL, 0xf938736417306c37UL },  // [ 5] N f1
                { 0x8549636fe204a2a3UL, 0x82b04d540225279cUL },  // [ 6] N g1
                { 0x2ec25b0adb2484e8UL, 0x89aabe99a7b4bc96UL },  // [ 7] N h1
                { 0x2c123d4134237abfUL, 0x1926e5f8b81b4122UL },  // [ 8] N a2
                { 0xdde87d8e36442486UL, 0xad965256049a0120UL },  // [ 9] N b2
                { 0x70be31f0da318efaUL, 0xb7d43bd90f55fdb2UL },  // [10] N c2
                { 0x0b849baa1e210367UL, 0xf80886f88a69ec71UL },  // [11] N d2
                { 0x486c81a88f3b8111UL, 0x2aececc2ab99af1fUL },  // [12] N e2
                { 0x387db9efef26476dUL, 0xf724033029344188UL },  // [13] N f2
                { 0x3f643e88692e6569UL, 0x4cd12b05f2f893b5UL },  // [14] N g2
                { 0xcc49784b3f81371aUL, 0x811767479c69f94fUL },  // [15] N h2
                { 0xf34e2e72a528c3e1UL, 0x582b68a90e683c6dUL },  // [16] N a3
                { 0x662c945805cd0f49UL, 0x386f52364b6cb44fUL },  // [17] N b3
                { 0xbf447b400777b903UL, 0x0600aae1ecfcec20UL },  // [18] N c3
                { 0xb2c5b3f80632fe86UL, 0x26ce1cb12b6c7a1dUL },  // [19] N d3
                { 0x347b5427949cdf19UL, 0xaef884af3da45322UL },  // [20] N e3
                { 0xc73836cf08ab7661UL, 0xe8b5b1eb60ea782fUL },  // [21] N f3
                { 0x6f8a8b9f0c20f709UL, 0x7b216e2019ef5ae8UL },  // [22] N g3
                { 0x6fe77b6dfe962956UL, 0x5dacbeca582aa056UL },  // [23] N h3
                { 0xdab24e98ba9ce7f1UL, 0x8beb9b6e59ca6bf0UL },  // [24] N a4
                { 0xd227e1cb98957934UL, 0xa49b17abea5a35b1UL },  // [25] N b4
                { 0xa98027a6a00f62f9UL, 0x673a8abadc877476UL },  // [26] N c4
                { 0xae6c24621d316081UL, 0xd740d2f09421f75bUL },  // [27] N d4
                { 0x9af116b82b2aabe3UL, 0x0bc1bd418e8138a9UL },  // [28] N e4
                { 0x19ca26e23ca912b6UL, 0x2335171e3a078650UL },  // [29] N f4
                { 0x43529c52e8476203UL, 0xa89cfc8c1b4123e4UL },  // [30] N g4
                { 0xfc24909edcc4dd1aUL, 0xede9286762293ebbUL },  // [31] N h4
                { 0x403815878be51468UL, 0xe4db582c1798f26dUL },  // [32] N a5
                { 0xd5df7531ff4c6dd4UL, 0x4557357ece94387eUL },  // [33] N b5
                { 0xdf5b053f5f164010UL, 0x174483f1c869a42cUL },  // [34] N c5
                { 0x2d7214aa8faa9d2fUL, 0xad22793b86860bc4UL },  // [35] N d5
                { 0x2487eb66219da1b0UL, 0x4f5fe0babed91239UL },  // [36] N e5
                { 0xc22406461ce95ffdUL, 0x2b747528b01ee495UL },  // [37] N f5
                { 0x7721ce508f27dd1eUL, 0xa6ace6b8b287314fUL },  // [38] N g5
                { 0xa739ac1a5659d674UL, 0xa86650d71234230dUL },  // [39] N h5
                { 0x1d24155754c691d2UL, 0x1ffb2d5fccb5ffcbUL },  // [40] N a6
                { 0xe4abb894d359731fUL, 0x2ce2ed9f24d2a3a6UL },  // [41] N b6
                { 0xc7590e6138b917fdUL, 0xe3d2f2cfcd543509UL },  // [42] N c6
                { 0xfa38112142eaa7aaUL, 0x879bcf3065d6ba80UL },  // [43] N d6
                { 0x1937be97a20159d4UL, 0xb9399a46773c6348UL },  // [44] N e6
                { 0x467585cd729d601eUL, 0x29e7722b51b0e50cUL },  // [45] N f6
                { 0xd1968e46b437fc75UL, 0xa3ebc0f9f3ef0e57UL },  // [46] N g6
                { 0xa195b386f9ca85e1UL, 0x4b5f1de8aa418ed8UL },  // [47] N h6
                { 0xe67e04093b46c7d4UL, 0xb9fd198ee90bd21fUL },  // [48] N a7
                { 0x176b71972bb57bf3UL, 0x877cde08abdef2b0UL },  // [49] N b7
                { 0x54522981ac642ba4UL, 0x9c252309ff39f867UL },  // [50] N c7
                { 0x200b4d9de7796111UL, 0x5fa2528d2f28bc33UL },  // [51] N d7
                { 0xb16a4a44724708bcUL, 0x1dcf98709f797adaUL },  // [52] N e7
                { 0x2c04c49200a4bf1bUL, 0x37382534886f7a5aUL },  // [53] N f7
                { 0x89272e13eae9c5f9UL, 0x715915cf84402152UL },  // [54] N g7
                { 0xdb2c747d293ff8bbUL, 0xe17611280724608eUL },  // [55] N h7
                { 0x0bec38d9e9560369UL, 0xdaec579ec230d81cUL },  // [56] N a8
                { 0x42dbf396bc5293f8UL, 0xf5f5cfbe7892e801UL },  // [57] N b8
                { 0xd677b86be3262573UL, 0xb33cfabdef056a74UL },  // [58] N c8
                { 0x8f79f93b754f3de0UL, 0xb7b19724f361b59eUL },  // [59] N d8
                { 0xd0d2470ece024aecUL, 0x14f106d9d6f5f88fUL },  // [60] N e8
                { 0xa23f785ea3643f52UL, 0xa3dfe1c02525d28dUL },  // [61] N f8
                { 0x5ebdb8fee5cb5db6UL, 0xa1e76be5b5da3a05UL },  // [62] N g8
                { 0x5f5006f30ffbddf4UL, 0x9fdb317caa8fdcfeUL },  // [63] N h8
            },
            {
                { 0x767c91ed540fcebaUL, 0xece2eb065bd3f44aUL },  // [ 0] B a1
                { 0x917835feeb03fbecUL, 0x5b803986d9464ce0UL },  // [ 1] B b1
                { 0x3861b2125d374601UL, 0x5e41964253dc0998UL },  // [ 2] B c1
                { 0x4373e21c0d421b27UL, 0xc6b3fa15049c7468UL },  // [ 3] B d1
                { 0x21221b259376b3eaUL, 0xf6775a04ccd211afUL },  // [ 4] B e1
                { 0x02ec0861ab998a79UL, 0xf328296e652d3fddUL },  // [ 5] B f1
                { 0x83d6b67ac642f8bdUL, 0x12d1c3f3755366f3UL },  // [ 6] B g1
                { 0x7d8295dd3ae99be0UL, 0xb76b5d18ef4a8eeaUL },  // [ 7] B h1
                { 0x6785b0ec8c6c928eUL, 0xeaa1e2bac64b0749UL },  // [ 8] B a2
                { 0xbc4fbedab94ec903UL, 0x29ebb6c3b2d95af1UL },  // [ 9] B b2
                { 0x7d2a19923f26ac3aUL, 0x34516319fd843ae7UL },  // [10] B c2
                { 0xd6404e55db903bb7UL, 0x45101085d132d4e4UL },  // [11] B d2
                { 0xf826d12f6f9fcd5cUL, 0x1a102a1e78e62c38UL },  // [12] B e2
                { 0x345ac4890ba1fdf8UL, 0x3573af074bfaf603UL },  // [13] B f2
                { 0xe0320995ca27b083UL, 0x8c34c468fb983471UL },  // [14] B g2
                { 0xe06779759b2264fdUL, 0x9a2713e90cbc87b5UL },  // [15] B h2
                { 0x696a52d2d961df45UL, 0x83524dd211be1760UL },  // [16] B a3
                { 0xbc2a2dce9b3fd4b6UL, 0x383aedbde0c74d64UL },  // [17] B b3
                { 0xcd2cf1f85208ae42UL, 0x1ff4cdbe803b3cc4UL },  // [18] B c3
                { 0x6d2f17466623f37bUL, 0x24c6daf21a98fd76UL },  // [19] B d3
                { 0x8d28ed3c36c9e6feUL, 0xb94d9106c8e25171UL },  // [20] B e3
                { 0x014367d0eab862c4UL, 0x8abbd23c61e66c6cUL },  // [21] B f3
                { 0x69072b83fc29ce0fUL, 0x927e7e91f864c9f8UL },  // [22] B g3
                { 0x733c5394e2c8c4d3UL, 0xa5bdca9031d509ecUL },  // [23] B h3
                { 0x373e8a6897d7324dUL, 0x876f355b0b36b2c8UL },  // [24] B a4
                { 0x4656bdafa0e0564bUL, 0xfd574b910e4d3875UL },  // [25] B b4
                { 0x12ae69be61c22af3UL, 0x00aa9ef8a5f5eaceUL },  // [26] B c4
                { 0xfa0d5a9acbfae0b4UL, 0x571db83fbb1154d7UL },  // [27] B d4
                { 0x2a31fc01c53a8290UL, 0x0987cbb271067d0aUL },  // [28] B e4
                { 0xdaea4c0cfac4b646UL, 0xbb5dbff7a249b8b2UL },  // [29] B f4
                { 0x07dfe7a920dd4553UL, 0x74441f1ef7fafebdUL },  // [30] B g4
                { 0x04d36d02d2712185UL, 0x910dc188541a73d0UL },  // [31] B h4
                { 0xb9a892ad918fbf16UL, 0x4f526c06530d7398UL },  // [32] B a5
                { 0xab6b2cace7c44f97UL, 0x99d0c7db63e3ffcbUL },  // [33] B b5
                { 0x91d37dc4eaedc9bdUL, 0x2c8f3dabf05d7314UL },  // [34] B c5
                { 0xe07b890804ae2c8fUL, 0xf27d251952c8da1bUL },  // [35] B d5
                { 0xff3bdd1101d1ce59UL, 0x6af5759f7e106208UL },  // [36] B e5
                { 0x2915c3ceb89e2bdaUL, 0x04136ec560e2ccceUL },  // [37] B f5
                { 0xbd93f6eb34bcca09UL, 0xec5c71c5eeb31fbdUL },  // [38] B g5
                { 0x4c8768d0f1a776a6UL, 0x55fe109f46ec477fUL },  // [39] B h5
                { 0x4d09fc47a318da59UL, 0xede1c84eb070e630UL },  // [40] B a6
                { 0xffd287574479235fUL, 0x2fe38ae30f7269a7UL },  // [41] B b6
                { 0x4a8eae89c1814ff1UL, 0xb2570672dfbeb229UL },  // [42] B c6
                { 0x285ac191861395b3UL, 0x6d1666b1456fa696UL },  // [43] B d6
                { 0x8534382981fc1c10UL, 0x111147d428e3571eUL },  // [44] B e6
                { 0x214a322b20fc2e43UL, 0xb35915eeece7308fUL },  // [45] B f6
                { 0xfa8f1cc8c00727d7UL, 0xca290367064e8d60UL },  // [46] B g6
                { 0xb75151cd6966c00cUL, 0x4ae7713f1b57d6f1UL },  // [47] B h6
                { 0x69aadfec86842265UL, 0x405914608a646eb1UL },  // [48] B a7
                { 0x587a2ead0aaf9c09UL, 0xd115d63babb4f94fUL },  // [49] B b7
                { 0x351a0fa79aadfdb4UL, 0xb4088542fd448e4eUL },  // [50] B c7
                { 0xadbafdeb7ae261e5UL, 0xfb45e36c18e8b224UL },  // [51] B d7
                { 0x8c9257d5e375c8f4UL, 0x3b9cd13a93c63338UL },  // [52] B e7
                { 0xf521664a84af5c6cUL, 0xc6a7217e423879adUL },  // [53] B f7
                { 0xb06be8dbfcd989d4UL, 0xff600b0846b9a12eUL },  // [54] B g7
                { 0x7896aee1c5faeee7UL, 0x4a266566e0fe3865UL },  // [55] B h7
                { 0x7ee65880c2cb6948UL, 0x5cf58133ae623fc1UL },  // [56] B a8
                { 0xb14b0eee85767d57UL, 0x0f6d0239397efcf6UL },  // [57] B b8
                { 0x9da92ff9d98bad22UL, 0xd6fb8d26666ec09aUL },  // [58] B c8
                { 0x2a2c41e524ebc600UL, 0xa08ddb7b0f970545UL },  // [59] B d8
                { 0x8794a16f90515616UL, 0x74ba8d6c45cf15e3UL },  // [60] B e8
                { 0x62dacebc7cc89aa9UL, 0x04ba3a11b25b070cUL },  // [61] B f8
                { 0x060111d30891bcc7UL, 0xe428ee3c4ebbb626UL },  // [62] B g8
                { 0x64289e8c6fbb014dUL, 0x578352ea36a864e1UL },  // [63] B h8
            },
            {
                { 0xb3acfb10886a7ebcUL, 0xa369dcc6bf95f3a3UL },  // [ 0] R a1
                { 0x5a05ec002817a693UL, 0x10b9ebab93d7e973UL },  // [ 1] R b1
                { 0x981b2693e1ef6806UL, 0xfebd6fbb17de04e4UL },  // [ 2] R c1
                { 0x4fffbce6ba1a2f82UL, 0x054a1b0111ce6be5UL },  // [ 3] R d1
                { 0x6227f4466f2a8adaUL, 0x0ff0c1eb429e0754UL },  // [ 4] R e1
                { 0x5683fef2ad6380b0UL, 0x8354ccc5403a8d94UL },  // [ 5] R f1
                { 0x818b3b299269825bUL, 0x47ad51fa56d88231UL },  // [ 6] R g1
                { 0xde7703723211b85aUL, 0x3766533e6780f157UL },  // [ 7] R h1
                { 0x3c7c64208244c7f8UL, 0x34e6a7d40385f5a9UL },  // [ 8] R a2
                { 0x4e65c85f45ee47cdUL, 0xfcaf46dc7ebef5b3UL },  // [ 9] R b2
                { 0xa59f6d827937425aUL, 0xc8c8f146f521e71eUL },  // [10] R c2
                { 0x6c991a7822c037a1UL, 0xa833978dfa2db714UL },  // [11] R d2
                { 0x54fd52efc7aaebc6UL, 0xa977904e79a0256aUL },  // [12] R e2
                { 0x9fc88b2652e10e73UL, 0xfdf95a31c0f06522UL },  // [13] R f2
                { 0x2fd649f2afac1926UL, 0x20f413f397067c70UL },  // [14] R g2
                { 0x795fbaa600179967UL, 0xe65edb14e83e321fUL },  // [15] R h2
                { 0x7119f6ae8c00ba38UL, 0xfc9d423b44cf82b6UL },  // [16] R a3
                { 0xe29e8522d66ce987UL, 0x91134d527437b659UL },  // [17] R b3
                { 0x8d91fb3231d7a49cUL, 0x60523b4a691856ccUL },  // [18] R c3
                { 0xbc33af25a0890b8fUL, 0x24340a254dbb9fd6UL },  // [19] R d3
                { 0xc69ff5c4580c4a68UL, 0x92a068adebe13bb6UL },  // [20] R e3
                { 0x8466ba1c07e6b3e7UL, 0xb398d9616174dc4aUL },  // [21] R f3
                { 0xb7d1592686224587UL, 0x8fa655525ca87a96UL },  // [22] R g3
                { 0xe39fb1d8bb19d284UL, 0x1eb74582801357faUL },  // [23] R h3
                { 0x2a5105aa83662474UL, 0x8353ba2683562e7eUL },  // [24] R a4
                { 0xb4b798afc95a40f9UL, 0x7bfe58f9e7657a53UL },  // [25] R b4
                { 0x9719f9d676a1d67eUL, 0xb5f3efb3f577a2b4UL },  // [26] R c4
                { 0x357b518fcf7a69e9UL, 0x7dddd9915811392bUL },  // [27] R d4
                { 0x5639c100a354f49cUL, 0x2386083fdadded94UL },  // [28] R e4
                { 0xd08686ba0c758c30UL, 0x41d490a709ca2c56UL },  // [29] R f4
                { 0x01a1e95cdea21cb1UL, 0xbb3cd90f8c3e3946UL },  // [30] R g4
                { 0x6e432280a9f8dcdbUL, 0x1303fbe070dcaed1UL },  // [31] R h4
                { 0x310cb6357d99b601UL, 0x7bd908e20b87b449UL },  // [32] R a5
                { 0x520d768e52d05252UL, 0x9c5f6074816e8c2cUL },  // [33] R b5
                { 0xa008222bccbd1261UL, 0x41e78dc147bf077cUL },  // [34] R c5
                { 0xfab3af09dafdbe09UL, 0x3102dcb3e39d1ee7UL },  // [35] R d5
                { 0x4e98012b8b5f96dcUL, 0xbcde74137d85da8bUL },  // [36] R e5
                { 0x51755ba0522f8be5UL, 0xa9cec84c92901061UL },  // [37] R f5
                { 0x5ab7d53c185a0ec7UL, 0x8f03359849c6debaUL },  // [38] R g5
                { 0x12c25d7bb2b87542UL, 0x700f300256985640UL },  // [39] R h5
                { 0xa1b0de53e1bc1ddeUL, 0x0c5092665ceaff78UL },  // [40] R a6
                { 0xda6c3ae08452717dUL, 0x2561d35849ad6e88UL },  // [41] R b6
                { 0xe09738bf3139f409UL, 0xac87fb3c95a87838UL },  // [42] R c6
                { 0x9284656668071a98UL, 0xc1571b60f3b6e89cUL },  // [43] R d6
                { 0x6a4dcaed6dd19ccfUL, 0x4af3c7981d42e063UL },  // [44] R e6
                { 0x8347dbce8db3839aUL, 0x09a723e5be90d213UL },  // [45] R f6
                { 0x2c719e080a9e5d23UL, 0xe7a142ce0e782773UL },  // [46] R g6
                { 0x7a14660ea7209315UL, 0x48bd5e6ce1de8672UL },  // [47] R h6
                { 0xb56b917cb7a2f2deUL, 0x209912df7f13a18cUL },  // [48] R a7
                { 0x6cfbafd1040870b1UL, 0xe4af62f56db0e2c9UL },  // [49] R b7
                { 0xb8a03011d85247c6UL, 0xbe91280d18cd63a8UL },  // [50] R c7
                { 0x5eadd59d21162603UL, 0xd4470cfb08d5282cUL },  // [51] R d7
                { 0x13d3ba92fcd7c744UL, 0x3a5387f8fc17b00eUL },  // [52] R e7
                { 0x81ce4cafe352750cUL, 0x720e1ca33bb4b1d7UL },  // [53] R f7
                { 0xccf359ad9ba95763UL, 0x7b7534c3a646c998UL },  // [54] R g7
                { 0xd30d76c21ddb8ee0UL, 0x985e4d780c7c27c0UL },  // [55] R h7
                { 0x88244172d9720245UL, 0xf0de6116f8a6cc00UL },  // [56] R a8
                { 0xd35b9809207e710bUL, 0xc0d67fa77c9b4921UL },  // [57] R b8
                { 0xb9756cde5039f04cUL, 0xd85d8c3222d38dcdUL },  // [58] R c8
                { 0x80d8f6b3481d0a62UL, 0xadea4886a8909f5fUL },  // [59] R d8
                { 0x7387dd76746f92baUL, 0x76d5ddaef4a0211dUL },  // [60] R e8
                { 0xd8af1f3178df54b7UL, 0x7d179ea909d11b8fUL },  // [61] R f8
                { 0x8ffc2f6fbba9c5faUL, 0xfd7252fa7ed171c7UL },  // [62] R g8
                { 0x3c55670707af4bc4UL, 0xe1e9cbf496e7417dUL },  // [63] R h8
            },
            {
                { 0xa80669f68eb77e08UL, 0x6e2ecd558cfb0f74UL },  // [ 0] Q a1
                { 0x9b960905ab722781UL, 0x7bae230a9e8161d9UL },  // [ 1] Q b1
                { 0x13153a6efb56e422UL, 0x26b22cf1b8104c75UL },  // [ 2] Q c1
                { 0x17397db43a56b06cUL, 0x880cbac986f729feUL },  // [ 3] Q d1
                { 0x696ac3aa8a8efc2bUL, 0x1eea461156dfa800UL },  // [ 4] Q e1
                { 0x211f50ed307dcc88UL, 0x4ee87b98079f0f21UL },  // [ 5] Q f1
                { 0xa0c01181309775afUL, 0x869f9137489f935eUL },  // [ 6] Q g1
                { 0xb547a41d0ab6895fUL, 0xe059927bf6a45e4bUL },  // [ 7] Q h1
                { 0x30cd074f4b584425UL, 0x359e035333223a68UL },  // [ 8] Q a2
                { 0x815101c5b0177c1bUL, 0x8c99001d609ffc87UL },  // [ 9] Q b2
                { 0xc8672720964eb188UL, 0x8d73e138d822241fUL },  // [10] Q c2
                { 0x3ef4712c5290b2afUL, 0xaac9eeca16e1c178UL },  // [11] Q d2
                { 0x54f5aeecb8085914UL, 0xa5401446636be13cUL },  // [12] Q e2
                { 0xf646ddca9027d268UL, 0xca9ed3871c213fd2UL },  // [13] Q f2
                { 0x18a4c2bc9733a5f8UL, 0x8ca4699f90d7e05eUL },  // [14] Q g2
                { 0x032db4472b34201eUL, 0x349ef26fb92608d3UL },  // [15] Q h2
                { 0x7c334e627bc732ecUL, 0x6ef06e48c4ba85acUL },  // [16] Q a3
                { 0x4b1a00301673f533UL, 0x8c7264de728c216bUL },  // [17] Q b3
                { 0x862752f252dcce0cUL, 0xd347ea354b28e25dUL },  // [18] Q c3
                { 0xcb1cdb40c39f4cd0UL, 0x4ad9f2856f90cd11UL },  // [19] Q d3
                { 0x683a135539979398UL, 0x3c82d81873d385a4UL },  // [20] Q e3
                { 0x82a551e499590958UL, 0xa27f3ed6afd9ef92UL },  // [21] Q f3
                { 0x6d048c177fe6caf7UL, 0xa3390ee8a1121993UL },  // [22] Q g3
                { 0xaaeb0f6b74ed95a7UL, 0x43748857848138e7UL },  // [23] Q h3
                { 0x5852420fb8da5317UL, 0xe93ceb47470d01dbUL },  // [24] Q a4
                { 0xd257a956863ad666UL, 0x4be205d4c77a316aUL },  // [25] Q b4
                { 0x5d67ce0f29acc0a4UL, 0x01f50348a3d68848UL },  // [26] Q c4
                { 0xa5b39ec7c3a60f3fUL, 0x740f2c96f67a293eUL },  // [27] Q d4
                { 0x588cd63ce97165f0UL, 0x41ede8e6fc43aeceUL },  // [28] Q e4
                { 0x8210acd014decf59UL, 0x6b45fe767544a2caUL },  // [29] Q f4
                { 0x7cfc3b0715ca4390UL, 0x1b21dd0a01d72f46UL },  // [30] Q g4
                { 0x92e4111564bdc0b7UL, 0x11129b2b437e27e7UL },  // [31] Q h4
                { 0x147cb94f8502f061UL, 0xded65fdfd00ec04cUL },  // [32] Q a5
                { 0x14bc0dc464aa3217UL, 0xc55d828ffbdc2c27UL },  // [33] Q b5
                { 0x5bc04ddf2f9c3122UL, 0x43090497fda3a6a6UL },  // [34] Q c5
                { 0x5f9e0548501a1bfdUL, 0x0fee44af23148c9eUL },  // [35] Q d5
                { 0x4a414a87acdf1fbeUL, 0xdec2f6a506cb8a75UL },  // [36] Q e5
                { 0xdfc13fb9e75add4fUL, 0x38d3c050550c650aUL },  // [37] Q f5
                { 0xf5c08d80c038af7dUL, 0x5e891ad15f27b327UL },  // [38] Q g5
                { 0xa4fe3c1460d99e80UL, 0x6fc770adab14a6feUL },  // [39] Q h5
                { 0x1c116442720557d1UL, 0xf53c8c984941d7f7UL },  // [40] Q a6
                { 0xe12f2e715704c4aeUL, 0x83ff9c004acf9df4UL },  // [41] Q b6
                { 0x64eef1f2c5fa76a1UL, 0xf3f802e29c254950UL },  // [42] Q c6
                { 0xef4500e3b09fe69fUL, 0x8b76f71b6b3b7b96UL },  // [43] Q d6
                { 0x3988babf42881e96UL, 0x54e799541d45192cUL },  // [44] Q e6
                { 0x322115c5d931920eUL, 0x0cda86417274d598UL },  // [45] Q f6
                { 0x33a35b918045f479UL, 0x49638eb4a937d28eUL },  // [46] Q g6
                { 0xa15bd34f69e18b22UL, 0x193291e5aea478aaUL },  // [47] Q h6
                { 0x7624ccc47acb0473UL, 0xb3a1e3e4335a8613UL },  // [48] Q a7
                { 0x670f730072ef2f11UL, 0x5e2869da23e257a7UL },  // [49] Q b7
                { 0xcb62e1a1a0a508eeUL, 0x413e91c260af2f60UL },  // [50] Q c7
                { 0x7ffe5d95d5194515UL, 0x50e2460d2e7f9f51UL },  // [51] Q d7
                { 0x38d3c23372bfbfffUL, 0x26e568167568ce1bUL },  // [52] Q e7
                { 0x8fa7d7c999229c19UL, 0xf1c19a4a65158fc2UL },  // [53] Q f7
                { 0x181ae389212f72d6UL, 0x7dd4b50ec67ba265UL },  // [54] Q g7
                { 0x7a3a00e952ee73deUL, 0x5b952a717df5edfaUL },  // [55] Q h7
                { 0x62b4a30f703282a5UL, 0x427c998ba4e7d7aeUL },  // [56] Q a8
                { 0x33aaf922e25bee2dUL, 0x9339619ed9abbcc0UL },  // [57] Q b8
                { 0x365de7034c38890aUL, 0xe70363430e6afcc7UL },  // [58] Q c8
                { 0x2ac76fc9b7430ef0UL, 0xea49bea2e4878852UL },  // [59] Q d8
                { 0x895941361de0a90bUL, 0x85007260deef4e3cUL },  // [60] Q e8
                { 0xc554e55c9f4294f1UL, 0x1cbb94bbe9915944UL },  // [61] Q f8
                { 0x78a41550350368d6UL, 0x51c8473f01afaabeUL },  // [62] Q g8
                { 0x069680d9d10ab761UL, 0x6dd0b56d49d0046bUL },  // [63] Q h8
            },
            {
                { 0xd25e8d2577fc6b86UL, 0x5be275c8375404a1UL },  // [ 0] K a1
                { 0x6f8a98e2af1d6854UL, 0x0ab835fb0cdb532bUL },  // [ 1] K b1
                { 0x46b34d6b11798badUL, 0x5056d939a767fae1UL },  // [ 2] K c1
                { 0x3de7ceb9280aba11UL, 0xe78713d6fd83982fUL },  // [ 3] K d1
                { 0x891535560a445eadUL, 0x37cc9e8b4fcdfc74UL },  // [ 4] K e1
                { 0x8d6aba0813aed3eeUL, 0xe4b86304ed3c9e76UL },  // [ 5] K f1
                { 0x8f3d305dcabd2b03UL, 0xddf799d9e9485b45UL },  // [ 6] K g1
                { 0x34092ed3a0cc8119UL, 0x46662b29d828826aUL },  // [ 7] K h1
                { 0x260419b8e45d0774UL, 0xf13eed8bfd37b162UL },  // [ 8] K a2
                { 0xad64d0a0261dc90dUL, 0xd00f25b6bdbf0079UL },  // [ 9] K b2
                { 0x51ec1eeb119bd640UL, 0x96354a2288f0f1a1UL },  // [10] K c2
                { 0x4ae76ac5828897a7UL, 0xc7dff0ccaa0c37aaUL },  // [11] K d2
                { 0x10168de3a6a13631UL, 0x9a61517a9ebf20bbUL },  // [12] K e2
                { 0x86a5a4c3d004cfcdUL, 0x92f1b9d518a70b9bUL },  // [13] K f2
                { 0x8df7e06c9b766f59UL, 0xa81623dc2c8292a2UL },  // [14] K g2
                { 0x69108771b0087b62UL, 0x6ff0d34bd5b479dbUL },  // [15] K h2
                { 0x5bdf75d378b51de8UL, 0xff6f7a2981527c50UL },  // [16] K a3
                { 0xa7e710008ecb1792UL, 0x99a12bae821e6701UL },  // [17] K b3
                { 0x774258478573e0c6UL, 0x789b3c18013a2f5aUL },  // [18] K c3
                { 0xad71a344d172ee81UL, 0xb405cd52d7d9ee26UL },  // [19] K d3
                { 0x6d6da41636bece23UL, 0x0a5e25cbeaf3202fUL },  // [20] K e3
                { 0xd0812fc9d3dfa59dUL, 0x34ab1bb3866e59c0UL },  // [21] K f3
                { 0x7c8ea65152845237UL, 0x2ca845d1b1e3009cUL },  // [22] K g3
                { 0x9797cb10885e6546UL, 0xdc43acea60bbc41fUL },  // [23] K h3
                { 0x61f6f1f4b83f2c53UL, 0x5896b1518e6e27bbUL },  // [24] K a4
                { 0xd2cc85c4755e6fdbUL, 0xc20351bb2f5f95ceUL },  // [25] K b4
                { 0xdc42a9936aa4877dUL, 0x58e09f7d5c04d60eUL },  // [26] K c4
                { 0x8d8b3bee960b89d8UL, 0xa9d9043146440c85UL },  // [27] K d4
                { 0xb6be96908f8b54d8UL, 0xbde6b3c58e2241beUL },  // [28] K e4
                { 0xc7faa18f9799b04aUL, 0xe2b7694be1d03a78UL },  // [29] K f4
                { 0xb6206db6cc1408cbUL, 0x83f60e4033536adeUL },  // [30] K g4
                { 0x214f8bf7fbfa6725UL, 0x9651c33e929c4b2eUL },  // [31] K h4
                { 0x886e266495abb97cUL, 0x236274b0e16198f9UL },  // [32] K a5
                { 0x2321b05573c4a552UL, 0x9914994710229927UL },  // [33] K b5
                { 0xa41ce4a8168d15d5UL, 0x1e1fae1d1951b2aaUL },  // [34] K c5
                { 0x225ca4e5dfe9d10dUL, 0xf660c3d18fc8b708UL },  // [35] K d5
                { 0x933b37c0f3736d7fUL, 0xb324db468b1462f1UL },  // [36] K e5
                { 0x945dcff4aa79d16dUL, 0xf37a556ded75b7e8UL },  // [37] K f5
                { 0xd0e1c7a302e42b83UL, 0x8d09f073ef090c1aUL },  // [38] K g5
                { 0x3ab34e6b46968a69UL, 0xf648c34799e24022UL },  // [39] K h5
                { 0xbd7689198bef2e90UL, 0x3b9c6cef6e5e0002UL },  // [40] K a6
                { 0x19632e6f343d2e2aUL, 0x7e02296fe3c5c140UL },  // [41] K b6
                { 0xc5e01f9031553d43UL, 0x08d9b4579541b379UL },  // [42] K c6
                { 0x8974cdc26be543e4UL, 0x393644ed4897e6ecUL },  // [43] K d6
                { 0xe92a28a01261114aUL, 0xb0ba6c46a9f9ecb8UL },  // [44] K e6
                { 0x3194dc570d1f382eUL, 0x7fd8364386f5c8d3UL },  // [45] K f6
                { 0x06c3b02a24bd779cUL, 0x24380d9ccbb6e42cUL },  // [46] K g6
                { 0x69bf5b6221d9868eUL, 0x1473168c06babdc2UL },  // [47] K h6
                { 0x4c534fab6158e83aUL, 0xd708192d8e851ba6UL },  // [48] K a7
                { 0x73d75748b542138fUL, 0x905f2903eea6ea33UL },  // [49] K b7
                { 0x2bef72d99c3a102aUL, 0x5cd1045ce2b6c740UL },  // [50] K c7
                { 0x00272896097e4db7UL, 0x6215a9e870bf388dUL },  // [51] K d7
                { 0x77b86bdc433abc85UL, 0xde56f92a6127d585UL },  // [52] K e7
                { 0x07c444584b08ae53UL, 0x79c4df1041bf5744UL },  // [53] K f7
                { 0xf934212ffa35c7fdUL, 0x3ef4f335729adfa6UL },  // [54] K g7
                { 0x4c522f8a82b88b45UL, 0x497d0cf8918b74c8UL },  // [55] K h7
                { 0x1bf873d161b497ccUL, 0x119e3afc2e7f00e0UL },  // [56] K a8
                { 0xce7665ca55a17fe9UL, 0x31463b2b16dfde33UL },  // [57] K b8
                { 0xc43f35c589bdeac8UL, 0xfb7554638db05c97UL },  // [58] K c8
                { 0xc6c8dc09f44bde81UL, 0x70d541b9726ff359UL },  // [59] K d8
                { 0xfe3d752dfc16f704UL, 0x7100c6d11787e52bUL },  // [60] K e8
                { 0x9496d2b873ecc82dUL, 0x0a6af685d9abee59UL },  // [61] K f8
                { 0x80bb6eb69999c8f3UL, 0xa9dce4c50df3699cUL },  // [62] K g8
                { 0x7324a67f00733ae1UL, 0x1e714d54426dc93cUL },  // [63] K h8
            },
            {
                { 0x08fdad22585debc3UL, 0x1bbb394ebf11d656UL },  // [ 0] (not used)
                { 0xa701e64b90307ba3UL, 0x7f61d9a20d2588e9UL },  // [ 1] (not used)
                { 0x8e0599183af147c1UL, 0xecb0804db76ec53cUL },  // [ 2] (not used)
                { 0xe743b1bce3f4aad5UL, 0x9322b740cd1e4a8aUL },  // [ 3] (not used)
                { 0xba0d23df967efa56UL, 0xf47fa230fc2f520fUL },  // [ 4] (not used)
                { 0x0855ae9255b207d9UL, 0xe4f3f02142c6e822UL },  // [ 5] (not used)
                { 0xe740e5c9250cc5b2UL, 0xb5ed237827c7f916UL },  // [ 6] (not used)
                { 0x65c915a79fd3a44dUL, 0x921ea0c4c897e927UL },  // [ 7] (not used)
                { 0x7b12339e9b3f975cUL, 0x8e4c8b39f303dba1UL },  // [ 8] p a2
                { 0x54a21afa4420cdb0UL, 0xad9e9e7365d9074cUL },  // [ 9] p b2
                { 0x7aef82962fa0be7aUL, 0xd9ab3781464b5e00UL },  // [10] p c2
                { 0xba562881d2ee830dUL, 0x5bd2174651be1204UL },  // [11] p d2
                { 0x4cf8ee4dc54e7962UL, 0x78089f8227cb276fUL },  // [12] p e2
                { 0xfc1b4cd39cc6a029UL, 0x6fd33aab30d8666bUL },  // [13] p f2
                { 0x6f36290bd8956907UL, 0x6361a99fffb4db46UL },  // [14] p g2
                { 0x957aec638eece2a4UL, 0xce818c43aade8a69UL },  // [15] p h2
                { 0xae5f46d64e6a4bbeUL, 0xa9ba956c8e96d688UL },  // [16] p a3
                { 0x4485f72c37bca197UL, 0x740d59f88ab07147UL },  // [17] p b3
                { 0x09032ec5105601cbUL, 0xdb2db4d7dc510544UL },  // [18] p c3
                { 0xf0e27425b5b396b3UL, 0x6d1e1a4eb07b5cc0UL },  // [19] p d3
                { 0xf08fa8a8b741f3b5UL, 0x98019780b9af7a2bUL },  // [20] p e3
                { 0x02c143cf76d928e2UL, 0x5b70b3849609ab08UL },  // [21] p f3
                { 0xde108636ffe651e0UL, 0xbbc401748e63fc20UL },  // [22] p g3
                { 0x29e19c57865505c6UL, 0x51a05eb20841a53dUL },  // [23] p h3
                { 0xf2e08109806a0ec3UL, 0xc069840fa8cb3ec0UL },  // [24] p a4
                { 0x38d0ed874179121fUL, 0x924bea3072f7cb37UL },  // [25] p b4
                { 0xa88bd7bf7176dd22UL, 0xa6303494c0039381UL },  // [26] p c4
                { 0x3fa0cc8c6ef262eeUL, 0xcaca6cd519ad03b2UL },  // [27] p d4
                { 0x9c73f729c111b219UL, 0x45f204e2916da8a1UL },  // [28] p e4
                { 0xa2c08d6f5e56dc32UL, 0x6c9e9cfa9d1d91abUL },  // [29] p f4
                { 0x3ac880fb2378f156UL, 0xab62d1a81ce172f1UL },  // [30] p g4
                { 0xf3833bb0f795c8e8UL, 0x1e370437749c8461UL },  // [31] p h4
                { 0x51a4ec74bce7872eUL, 0x98519080624876bfUL },  // [32] p a5
                { 0xbcf8c19a46b6d3e7UL, 0xc312a85847f9da37UL },  // [33] p b5
                { 0x19483d421b4c1828UL, 0xf3818d9ea79970c5UL },  // [34] p c5
                { 0x16b4fba14e81e252UL, 0xa4ec1012741ee3f4UL },  // [35] p d5
                { 0x58b27d1e897c1ad7UL, 0x6885fa0e6407edbcUL },  // [36] p e5
                { 0x6b7bdf4affb63bdcUL, 0x2480d1b1ce0bdcbeUL },  // [37] p f5
                { 0x6da9e7e7235c42a9UL, 0x314f97acb5a36a71UL },  // [38] p g5
                { 0x4a8b011904b5f56fUL, 0x13cb9c1ada8b2456UL },  // [39] p h5
                { 0xe9a5a9b22fb1fd11UL, 0x7ed1aaecef16b390UL },  // [40] p a6
                { 0x10cc3e74356e89e9UL, 0x7d9759e49bc225c2UL },  // [41] p b6
                { 0x08f7c70a8d28f87dUL, 0xbfeced1a4ba6667bUL },  // [42] p c6
                { 0x9f2ca5f2ec3c559aUL, 0x64621ab2277ef732UL },  // [43] p d6
                { 0x4e331546b910ba1dUL, 0x43d0311ecbf37a26UL },  // [44] p e6
                { 0x89a7b733fa5c9cc5UL, 0xed846437634c3de9UL },  // [45] p f6
                { 0x7b60cb21e2345e47UL, 0x47cca833b1e3d60aUL },  // [46] p g6
                { 0x71a33afb9011cd72UL, 0x7fc5ba64687364edUL },  // [47] p h6
                { 0x7b922131d018b61aUL, 0x3ab914cf987a3d5dUL },  // [48] p a7
                { 0xb4d316375308adadUL, 0x3ea1465ddeb60369UL },  // [49] p b7
                { 0x9dc9119013342b4cUL, 0xf9a1466b11ab3848UL },  // [50] p c7
                { 0x4d7f63397f7f26dfUL, 0xa03907a30fd24e8cUL },  // [51] p d7
                { 0x1cdc5228ad4ba0e2UL, 0x84f21dae0b0fd3c5UL },  // [52] p e7
                { 0xc79b1c530c97a3f1UL, 0xa01609d85bf6dee9UL },  // [53] p f7
                { 0x3081ba7e5cc7b0eaUL, 0x78797e47210be8dcUL },  // [54] p g7
                { 0xaf7cefc28a8e0e43UL, 0x998f95f4b384ec51UL },  // [55] p h7
                { 0xa79d313e909b8d41UL, 0x36acb0ce9278404cUL },  // [56] (not used)
                { 0x31d314d514fd4743UL, 0x75e36c84563a78dbUL },  // [57] (not used)
                { 0xe2eec5a8873b9ab8UL, 0x22a2753ba65147b9UL },  // [58] (not used)
                { 0x7ecbf2a13c8d40acUL, 0xeb16963c1c24a034UL },  // [59] (not used)
                { 0x2b9310e2c5cd5d60UL, 0x72d9a50e4da6ae3aUL },  // [60] (not used)
                { 0x6416154d9239741bUL, 0x9d05e9301c519a29UL },  // [61] (not used)
                { 0x70110a85bd1fbd7eUL, 0x6767c2a16b22ecafUL },  // [62] (not used)
                { 0x0d30e0a4d8662433UL, 0x6204026a418fe385UL },  // [63] (not used)
            },
            {
                { 0x71a402fffc876751UL, 0x7440efc46773e31aUL },  // [ 0] n a1
                { 0xb0e8be239c268e4dUL, 0xe39d58914ae0da5dUL },  // [ 1] n b1
                { 0x46d6916a1465d895UL, 0xa76ca982892d5b99UL },  // [ 2] n c1
                { 0x3a5ca80bb94bfb80UL, 0x1bdc1ad3a94f458fUL },  // [ 3] n d1
                { 0x9fe610e3edcbbcf8UL, 0x8504a624139a8e59UL },  // [ 4] n e1
                { 0xd9c8492211ddbb19UL, 0xf1abc8397bceddf2UL },  // [ 5] n f1
                { 0xd5e9eb477ff64a81UL, 0xa578f84274c281a2UL },  // [ 6] n g1
                { 0xa0b97e6d2edd6ca4UL, 0x99e0afbb93d93b81UL },  // [ 7] n h1
                { 0x52247313d5bee543UL, 0x07b8fedb4f8c3a81UL },  // [ 8] n a2
                { 0x969f5cab458c737fUL, 0xc696564fd3ab557cUL },  // [ 9] n b2
                { 0xf8f84cddceb92216UL, 0xf720441906888ddaUL },  // [10] n c2
                { 0xcb08199f0cbe821dUL, 0x165bf8faab11908dUL },  // [11] n d2
                { 0x7badbf143fd38084UL, 0x721a8768a782e7a9UL },  // [12] n e2
                { 0xa67ec908784516c8UL, 0xf190bcc4a81aed7fUL },  // [13] n f2
                { 0x4d667e95977a92f6UL, 0x3c1f492aaac8754fUL },  // [14] n g2
                { 0xb0196df1209692f8UL, 0x06fb35fa6106e889UL },  // [15] n h2
                { 0x7b76498cc24a32a0UL, 0xa28beccee6c8e958UL },  // [16] n a3
                { 0xcbf38b1df199bd7cUL, 0xab138bdd25d170cfUL },  // [17] n b3
                { 0x967a7f7d1985c9d6UL, 0x69e7821e1be42cb4UL },  // [18] n c3
                { 0xc49cb5287348ae4cUL, 0x5ff45c51ddf49074UL },  // [19] n d3
                { 0x6772e94e3ec49bd6UL, 0x293b7601897e2c4fUL },  // [20] n e3
                { 0xaf5fabc383dc038dUL, 0x9413450dc7681aa6UL },  // [21] n f3
                { 0xc3a864a3007919b8UL, 0xf156d2649c483126UL },  // [22] n g3
                { 0x2cc444199f40c930UL, 0xf5fb815ae3994194UL },  // [23] n h3
                { 0x5665317b315aa673UL, 0xbb8e4157e5834d2eUL },  // [24] n a4
                { 0x8422d7d50639f1fbUL, 0xd9bc0ee2edaea841UL },  // [25] n b4
                { 0x71dae183a0032607UL, 0xbe38a27adc2d8f04UL },  // [26] n c4
                { 0x781f2e6a8b92234cUL, 0xc27377f4adffd04fUL },  // [27] n d4
                { 0x0504143f6ffe5805UL, 0x47374c1e172218b5UL },  // [28] n e4
                { 0x4f3327abe4b121afUL, 0x21951de003370cf2UL },  // [29] n f4
                { 0xaca8a9c78b686e82UL, 0x9f49772ce87983abUL },  // [30] n g4
                { 0x29d17f7b972dfc0eUL, 0x27799ac9115820a6UL },  // [31] n h4
                { 0xe5b33f4c8695013eUL, 0x97ef9f06ce19b91cUL },  // [32] n a5
                { 0x56f87707097c8485UL, 0x9d3bd7c09e2901acUL },  // [33] n b5
                { 0xcf4fa4f56a30fed5UL, 0x2f8aa6ed55338be1UL },  // [34] n c5
                { 0x3e8007e048f01d01UL, 0xdd210eae3c839c55UL },  // [35] n d5
                { 0xbaf671e57f2d0243UL, 0x6ad38014fe94d8e1UL },  // [36] n e5
                { 0xb06e05312948e571UL, 0x4ea384e7268a3863UL },  // [37] n f5
                { 0x61ef186fda65036bUL, 0x8c90e4052e7c7e65UL },  // [38] n g5
                { 0xb84238bcded79b23UL, 0x201bdea924b67addUL },  // [39] n h5
                { 0x91aa79e9e915608fUL, 0xae6a609996e3ad78UL },  // [40] n a6
                { 0x7cf8c27e2d616f7eUL, 0xdbab769be5e770c3UL },  // [41] n b6
                { 0x68d47f140d1d2cadUL, 0x72bb0098f4745ec5UL },  // [42] n c6
                { 0xec31bcaa5c3f7023UL, 0xdaabea35188d97fbUL },  // [43] n d6
                { 0xb1e38ccea5af9abbUL, 0x7064ce4d0e090e10UL },  // [44] n e6
                { 0x9aa9f168a4106e5aUL, 0x2f56f66470ef9f83UL },  // [45] n f6
                { 0x17684102b1144738UL, 0x771dcb54fd444854UL },  // [46] n g6
                { 0x44b670e62c149b5cUL, 0x22643539cc87a33fUL },  // [47] n h6
                { 0x9bcd1ede3bccb4f6UL, 0x3cd49739038145abUL },  // [48] n a7
                { 0x9ce4dcd1874f1767UL, 0xd55eb7ca03ba0b70UL },  // [49] n b7
                { 0xa3993531a9548c5cUL, 0x41a38e5c0b4659a4UL },  // [50] n c7
                { 0xd6893fd03d1e224eUL, 0x1e1dbd63f154e2b1UL },  // [51] n d7
                { 0x9ca5bb2033f3d363UL, 0xbadb165e3ac0dbbbUL },  // [52] n e7
                { 0x85f1c04a3d3ee922UL, 0xf080cb2bcc19f538UL },  // [53] n f7
                { 0x430445c5d37a8c1eUL, 0x0fa32d65a526842bUL },  // [54] n g7
                { 0x978c8500ad586410UL, 0x7d59bf00e0def992UL },  // [55] n h7
                { 0xac1b5f1cc40d92a2UL, 0x38b3a89ed7a9b295UL },  // [56] n a8
                { 0x64d760f3646cda4aUL, 0x9aed56e8c8da63ddUL },  // [57] n b8
                { 0x3cce0f518ddc040cUL, 0x2df749a5fd55a4b4UL },  // [58] n c8
                { 0x4d8ba53926de4eddUL, 0xfa7460e446717c34UL },  // [59] n d8
                { 0xa5b6ff69ccede19dUL, 0x2c89c523ac045ae5UL },  // [60] n e8
                { 0x3015ea8f61996663UL, 0x6c09b93b020f45d3UL },  // [61] n f8
                { 0x6effedbb14f9f4eaUL, 0x95917672c0ec668fUL },  // [62] n g8
                { 0xc350ecb071a1e6d9UL, 0xef31deebbbceabbdUL },  // [63] n h8
            },
            {
                { 0xfc4a68e8303e6777UL, 0x60c8156b7add3b22UL },  // [ 0] b a1
                { 0xdb8fee447f777589UL, 0x59fab77e47a86b0fUL },  // [ 1] b b1
                { 0x0f619b377e9eafb2UL, 0x56781802bca72b23UL },  // [ 2] b c1
                { 0xe3ce9cbc0ba99108UL, 0xf887ecafe5a28e81UL },  // [ 3] b d1
                { 0xd0084231e7005ee7UL, 0x2acf46a45301e9abUL },  // [ 4] b e1
                { 0x41206b76904db88bUL, 0xd8d67010638b2a78UL },  // [ 5] b f1
                { 0xd94af158cc19b706UL, 0x89d5d60206b4087eUL },  // [ 6] b g1
                { 0x77642cedd2994f1dUL, 0xc3b4fd4fefe23a5bUL },  // [ 7] b h1
                { 0x35a86baebfe7f8eaUL, 0x2be0e66f78962f5fUL },  // [ 8] b a2
                { 0xd622184551d2ea60UL, 0x0373d705c723494dUL },  // [ 9] b b2
                { 0xaa72a17033dcf3ceUL, 0x1c74c9f70dcabd6dUL },  // [10] b c2
                { 0x5d3c58a11e98b6c8UL, 0x07c9577d6064a1fbUL },  // [11] b d2
                { 0x1f73f0ef19679babUL, 0xec2ff7189f3d5c8fUL },  // [12] b e2
                { 0xb339c9573ecad5f9UL, 0xe79f8926b7999e87UL },  // [13] b f2
                { 0x637f7fb6e8860f5aUL, 0x9fec0e32d18d1b8bUL },  // [14] b g2
                { 0x5682983a3143fa6aUL, 0x823e5cd284a7f5a2UL },  // [15] b h2
                { 0xbd3650373e0a0f54UL, 0x8b0b72fb2c487996UL },  // [16] b a3
                { 0x8bfd602c45127157UL, 0xa08458f825a37b26UL },  // [17] b b3
                { 0xf5fcae962c5235d6UL, 0x431b2a3e04617121UL },  // [18] b c3
                { 0xbf35cde819f01b7aUL, 0x4bcf513bbb46f1beUL },  // [19] b d3
                { 0x5a89ade501d7268eUL, 0x28a49bc63e11aaaaUL },  // [20] b e3
                { 0xcfada9da3d41c5c2UL, 0xd8c5636ef80f41a5UL },  // [21] b f3
                { 0x79d5621cd8b37dd2UL, 0x67033b735001a2a6UL },  // [22] b g3
                { 0xb0d4a237144b5093UL, 0x2c09c6658f280bbcUL },  // [23] b h3
                { 0x9dd1bc955532647cUL, 0x1a1df0747ab51421UL },  // [24] b a4
                { 0x073d9363cd32bca6UL, 0x39b3d14d800a976eUL },  // [25] b b4
                { 0xa0ad4703624005f1UL, 0xefcea8f9c62d25e5UL },  // [26] b c4
                { 0xcee918d2e844ac75UL, 0xd8317f8509f9b73aUL },  // [27] b d4
                { 0xc90aa5daf3ddc471UL, 0x2d7dc5358036f115UL },  // [28] b e4
                { 0x09af4dcae55a9a93UL, 0xda3588d0fd2af657UL },  // [29] b f4
                { 0x7caa7b00ac6ec13aUL, 0xc46f29fbbed82f29UL },  // [30] b g4
                { 0xe9b9c82e7c4ca42aUL, 0x317d88e02424cef3UL },  // [31] b h4
                { 0x198df2943bab56f8UL, 0xc66be3c05808dda2UL },  // [32] b a5
                { 0x2b30dec54155f59eUL, 0x05d34fa435247cfdUL },  // [33] b b5
                { 0xc2b19d0dd8d7a7b9UL, 0x5c64886bfc069a03UL },  // [34] b c5
                { 0x03cef1e4de146a71UL, 0x1842c21849b89a3bUL },  // [35] b d5
                { 0x2119fb3128d07a2aUL, 0xddbd7be7b3fb3fb4UL },  // [36] b e5
                { 0x98ef22ba19233d7aUL, 0x2bbc4bbabfd7c03aUL },  // [37] b f5
                { 0x555eb825af6f0c66UL, 0x309dcd9f57b18337UL },  // [38] b g5
                { 0xdd0f95109ca6fee9UL, 0xcb71dd35b5d3dbfcUL },  // [39] b h5
                { 0x843ce1bf7f3c881cUL, 0x617a1c64da30516aUL },  // [40] b a6
                { 0x968461181455b475UL, 0x9f685c1f24adeab1UL },  // [41] b b6
                { 0xb08a2c838f1b40baUL, 0x762596e011646769UL },  // [42] b c6
                { 0xde49201e8f58f5e4UL, 0x3166ed1b9c928557UL },  // [43] b d6
                { 0x7b85fd144e713bd4UL, 0x9dd936f036fa2a38UL },  // [44] b e6
                { 0xc19db66f1eb88435UL, 0x175ffed26dc9eaa3UL },  // [45] b f6
                { 0x6292ea53d77795e5UL, 0xdf92b370415e2337UL },  // [46] b g6
                { 0x19d86894f3fb944bUL, 0x2319cfb52b654709UL },  // [47] b h6
                { 0x7982f515a88a2988UL, 0xeeb6e3531bf9b308UL },  // [48] b a7
                { 0x177908127ed9fe2dUL, 0x41279fb5ed0ee593UL },  // [49] b b7
                { 0x1a5feb6fa06460edUL, 0x4dd07cbf2b1ea78fUL },  // [50] b c7
                { 0x0aaca7fd3195a95dUL, 0x653c0389acc00612UL },  // [51] b d7
                { 0xcbad34bd380fe3daUL, 0xb4546e7698503d69UL },  // [52] b e7
                { 0xd9b77ca6b82b4e78UL, 0xf50a9558d314a8d6UL },  // [53] b f7
                { 0x6a5d6cd612668be2UL, 0xc31cba3cf314a629UL },  // [54] b g7
                { 0xdd4e7ccebe54881bUL, 0x581cbc48f2ea0858UL },  // [55] b h7
                { 0xb308ede77ea2def9UL, 0x5cf8bb12f91bc7c7UL },  // [56] b a8
                { 0x73b4a58334742087UL, 0x256c6324d839058cUL },  // [57] b b8
                { 0xf70826e461aff2c5UL, 0xeda4e4d4a86db5f1UL },  // [58] b c8
                { 0x6421269d72d74d42UL, 0x366fe2995dc7732fUL },  // [59] b d8
                { 0xe8e076ccab51b486UL, 0x4377d608b538ab04UL },  // [60] b e8
                { 0x5220f75a1b08b3c9UL, 0x6dfcf031e612eb66UL },  // [61] b f8
                { 0x8420f8e8e2ccc203UL, 0x49bb2faac4fe0c3bUL },  // [62] b g8
                { 0x138a3431f6ab831eUL, 0xc616d49c37222432UL },  // [63] b h8
            },
            {
                { 0x2b9ef71d685bb788UL, 0x059fdf1caacac098UL },  // [ 0] r a1
                { 0x205197b512b133e3UL, 0x5ba8ffbe7c153a4eUL },  // [ 1] r b1
                { 0x0bbff9a36c9dc53fUL, 0x16e3127cb1651f28UL },  // [ 2] r c1
                { 0xa6ae824cbf0a2cd7UL, 0x9ca6f8b32ca0d766UL },  // [ 3] r d1
                { 0x9d7c958dd8a2a3eeUL, 0x4b6e2a3c52ceeafaUL },  // [ 4] r e1
                { 0xb8dfd79650964615UL, 0xf64f8a68f19e01edUL },  // [ 5] r f1
                { 0xbc22a2a2ba3f12a2UL, 0x758605a5ed2595aaUL },  // [ 6] r g1
                { 0x1d8fdb7fd681571aUL, 0xc3be6c3ee5b2e59bUL },  // [ 7] r h1
                { 0x48f16050e5d5df04UL, 0xa5df5774533aaf49UL },  // [ 8] r a2
                { 0x0ffacffbe6e166bbUL, 0x07fc8fa8a6cfb1f7UL },  // [ 9] r b2
                { 0x836e284621109fa4UL, 0x4bf56876926440dcUL },  // [10] r c2
                { 0xba855dc2218beedfUL, 0xc78f4875f087ba7aUL },  // [11] r d2
                { 0xa01e14a87b7e010dUL, 0x4ef8be30cce39fceUL },  // [12] r e2
                { 0xb3d3845f1cc236a4UL, 0xef7af00cc822f9f6UL },  // [13] r f2
                { 0x3ee6d0fea2db9bd7UL, 0x9f6cafd76f397e78UL },  // [14] r g2
                { 0x74fdb4030ae95dd7UL, 0xc2ca658c909e0eeeUL },  // [15] r h2
                { 0x160be368b328f11eUL, 0x5bd38b0f1a85f7fcUL },  // [16] r a3
                { 0x1f737e067135a61cUL, 0x7404ed23a8f45fceUL },  // [17] r b3
                { 0x70d80d5155bf3a72UL, 0x15f73f6fc2e49f84UL },  // [18] r c3
                { 0x0a6dfd9a04e6bf2eUL, 0x64ad294375093be7UL },  // [19] r d3
                { 0xce0519a0d3f6a9b5UL, 0xb6c1c9c8ef3dc66fUL },  // [20] r e3
                { 0x9983dcd9f3e14d30UL, 0xca3903ac4d806c29UL },  // [21] r f3
                { 0xaed9d3928e0937b2UL, 0x274e99f6c16c0c16UL },  // [22] r g3
                { 0xc6f180b001b45900UL, 0xa628ddda73501be6UL },  // [23] r h3
                { 0x96674cc148a6a02eUL, 0x57e71862fa9b44c2UL },  // [24] r a4
                { 0x599b12f32b4a57dbUL, 0xc7bb937713aef907UL },  // [25] r b4
                { 0x114c86df1af75f6dUL, 0x3760cb0f9b288b6dUL },  // [26] r c4
                { 0xfdd28ff1744f3508UL, 0x7415e694569a116cUL },  // [27] r d4
                { 0x2fe99a22de594c09UL, 0x42c5ddc37a058c3dUL },  // [28] r e4
                { 0x72be67bea37506eaUL, 0x57056bae753fd4e0UL },  // [29] r f4
                { 0x33c97e938caa5c26UL, 0xb05bc6e2680f1b57UL },  // [30] r g4
                { 0x4b53527455390a3bUL, 0xb46d503008e3247fUL },  // [31] r h4
                { 0x1f369e4efd8331a4UL, 0x80790a7447ed1908UL },  // [32] r a5
                { 0x78e9b52229095bd9UL, 0x854c0bf76840bd8fUL },  // [33] r b5
                { 0x77b8280d41325a81UL, 0xdadd9a5c521e99e8UL },  // [34] r c5
                { 0x474c0a572454dad5UL, 0xa36c59a4e17205a7UL },  // [35] r d5
                { 0x7b268bee825d9ddfUL, 0x8ac115a45786be67UL },  // [36] r e5
                { 0xc1af7f6388d30204UL, 0xd528ef1963e9302eUL },  // [37] r f5
                { 0x9b1f2bde8b01209eUL, 0xc47cb097c215f5ecUL },  // [38] r g5
                { 0x1c354ade11a66fc9UL, 0xee25f4228a950cd4UL },  // [39] r h5
                { 0x67f2f682d0f2d529UL, 0xe87b7dad5298a692UL },  // [40] r a6
                { 0xe057b5922eaee374UL, 0xbd7d6fa5a9c0266eUL },  // [41] r b6
                { 0xa4516fac757ca30dUL, 0xf6d6d4b063e9cc81UL },  // [42] r c6
                { 0x7b85432705e1b651UL, 0x1eed53392e8c6c7bUL },  // [43] r d6
                { 0x8f8f871b94655999UL, 0x6315f71299ce5064UL },  // [44] r e6
                { 0x9f62859112fbf981UL, 0x886d57aeefd36745UL },  // [45] r f6
                { 0x14f8ddb3af6b54c8UL, 0xbda48e49574bcc97UL },  // [46] r g6
                { 0x96fe708b5683aeb8UL, 0x3df655774e2de75fUL },  // [47] r h6
                { 0x09f07089d7818f07UL, 0x30c0a14118713ddaUL },  // [48] r a7
                { 0x3f5178e634bc307eUL, 0x57ee492b01ed0f0dUL },  // [49] r b7
                { 0xb99fbed3ca4c57bbUL, 0xb0a998db91cce261UL },  // [50] r c7
                { 0xc2b52ac521126895UL, 0x3c7a68f697e78f63UL },  // [51] r d7
                { 0xa52fba4602f6cb55UL, 0x17e2f841c7360cfeUL },  // [52] r e7
                { 0x3191e647c087602eUL, 0xf66f8872a9b75b2dUL },  // [53] r f7
                { 0x18b7b9754e85dc09UL, 0xf3adee2760682a06UL },  // [54] r g7
                { 0xf8f98a12597e01bcUL, 0x08182b0eb27d2cb7UL },  // [55] r h7
                { 0x9e92d1189f6adc36UL, 0xa4f0554e1d605e09UL },  // [56] r a8
                { 0x3f14ec9960bf3d5cUL, 0x11171e9bfb471200UL },  // [57] r b8
                { 0x02772dac977513f5UL, 0x3c38ae91cf1ca9c4UL },  // [58] r c8
                { 0x6a4c7be3afa07054UL, 0x5c16bbdf7539e1abUL },  // [59] r d8
                { 0x9f7ff75bd305f770UL, 0xeff204d84a0bf82fUL },  // [60] r e8
                { 0x34c13253af0af5c2UL, 0xd8ced8346c8bf040UL },  // [61] r f8
                { 0xbdeed774ee5f6b66UL, 0x40f9ff4d99b96cdfUL },  // [62] r g8
                { 0xe0132871775b1c76UL, 0x87db129949f048d6UL },  // [63] r h8
            },
            {
                { 0x4a92e4c3ba7a4172UL, 0x398b293efe35985cUL },  // [ 0] q a1
                { 0xe055209f6d623635UL, 0xc52aceb1cd1d0f3aUL },  // [ 1] q b1
                { 0xdafb27ab46f372a0UL, 0x15a29eeeaf7abd49UL },  // [ 2] q c1
                { 0xf7910f3f0fd6f133UL, 0x94d63d7ea1d6c0f5UL },  // [ 3] q d1
                { 0x54b797cb0ef26431UL, 0x5d8f14375248df83UL },  // [ 4] q e1
                { 0xa5052dd5ab623ea4UL, 0x5d9f95ca25b8a2ebUL },  // [ 5] q f1
                { 0x33417b02cd0ec6acUL, 0xd968483bea5f8b54UL },  // [ 6] q g1
                { 0xe99377662fbfa641UL, 0xc78ca3e2ded55fc8UL },  // [ 7] q h1
                { 0x24c14178ecf45d20UL, 0x9434d8f20af70098UL },  // [ 8] q a2
                { 0xe511703d2ec55e27UL, 0xd99674debc9bc7fbUL },  // [ 9] q b2
                { 0xde8386a179f79211UL, 0xc432558fdfbd877dUL },  // [10] q c2
                { 0xa188698e66674ab7UL, 0xf8a5d81e7e06003dUL },  // [11] q d2
                { 0x001dfe0ea15196d2UL, 0x96e1737f904cf541UL },  // [12] q e2
                { 0x390e694d98a8e73cUL, 0x207134d513bdb397UL },  // [13] q f2
                { 0xaed9702eb2dd916cUL, 0x654909c619d3e43dUL },  // [14] q g2
                { 0x04442ac6aa9f2371UL, 0xa6543dedfff78052UL },  // [15] q h2
                { 0x5f0680399704a5c7UL, 0x4e9023bfc5db15b5UL },  // [16] q a3
                { 0x8120edc20219b55eUL, 0x43ca55e3c7560115UL },  // [17] q b3
                { 0xb299b096e8f945fdUL, 0x7d44af31cfb2350dUL },  // [18] q c3
                { 0x7ec3617de9a8cfedUL, 0xd444ee10bd439eeeUL },  // [19] q d3
                { 0x2a80eadd0d4313d0UL, 0x39960c2b43471c7eUL },  // [20] q e3
                { 0x31a4111971f35418UL, 0xa834f006ceda4467UL },  // [21] q f3
                { 0x7abe814841351c82UL, 0xefe74f5e7f9273b3UL },  // [22] q g3
                { 0x1a7ad5e39674f367UL, 0xf07689c4284c4e1bUL },  // [23] q h3
                { 0xb0884f3b0060a664UL, 0xbc07bd0d658277b1UL },  // [24] q a4
                { 0x33a67d57586bc685UL, 0xf2d3064a53405118UL },  // [25] q b4
                { 0x4999f0d2c2a157fcUL, 0xb6d63134a8a9da28UL },  // [26] q c4
                { 0x6b0903323f64cdceUL, 0x470adab531233834UL },  // [27] q d4
                { 0xf33c24c247a52827UL, 0x2c2d5f61bc34be69UL },  // [28] q e4
                { 0x388fc634bc4d4ae1UL, 0x57619a9d93f0b362UL },  // [29] q f4
                { 0x37fac615c7ee650dUL, 0xc10e008a0825ed4cUL },  // [30] q g4
                { 0xbbf798dcee36daf5UL, 0x5bc7b83cbaf55669UL },  // [31] q h4
                { 0x810ed3ce84b9bd96UL, 0x16679fde34b5aa72UL },  // [32] q a5
                { 0x06f02bab47fa0f57UL, 0x68f9397fa74f658fUL },  // [33] q b5
                { 0xac3cc7b86e404c65UL, 0x9d107a7abbdf5411UL },  // [34] q c5
                { 0x52fa21d97fd42cdcUL, 0x0904e2d4ad829825UL },  // [35] q d5
                { 0x4b6d65037631cbcdUL, 0xfbedb635c1a8f592UL },  // [36] q e5
                { 0xc435653321e48b9bUL, 0xf01b213ecf538e9cUL },  // [37] q f5
                { 0xd59f7f52ec671a11UL, 0x76dd8c3e8fd91e1eUL },  // [38] q g5
                { 0x32eb41ed6b678f23UL, 0xd9ce3f4c9f0df335UL },  // [39] q h5
                { 0x3daa00496fe5885bUL, 0x7aeb2e12d4be690bUL },  // [40] q a6
                { 0x51a65feef3f28a86UL, 0x07717ae20fac199bUL },  // [41] q b6
                { 0x366acdeac75c74a8UL, 0xe113a9c034f35db9UL },  // [42] q c6
                { 0x88ab9c8e9e81ca6fUL, 0x36582432afc548a8UL },  // [43] q d6
                { 0xccabd7c17935ca99UL, 0x0fa05530d3249ebfUL },  // [44] q e6
                { 0x673e0f3c92de607dUL, 0xfee6c29354f0162fUL },  // [45] q f6
                { 0x327599ecea98404eUL, 0xfb4a281465d7b0beUL },  // [46] q g6
                { 0x5572395f43f30383UL, 0xab05ae2652e543cdUL },  // [47] q h6
                { 0x61b0f3aede37f0b6UL, 0x40dc5c69bb91209cUL },  // [48] q a7
                { 0x5b7051ff4586a724UL, 0xa42b69efe0d21542UL },  // [49] q b7
                { 0x3d28194a1b6feab5UL, 0xf7ac087baac98321UL },  // [50] q c7
                { 0xd0b6d167d92b423bUL, 0xc29b7ce1b5051171UL },  // [51] q d7
                { 0x8a19babfc0b8e795UL, 0x843efa602d471914UL },  // [52] q e7
                { 0x2a81b2ab7b05e60dUL, 0xb1117cde3e9d31d7UL },  // [53] q f7
                { 0x791db5b487bd6231UL, 0xb386ecb0ae4e4808UL },  // [54] q g7
                { 0x9900644c8228fd82UL, 0x1cf311278206ff90UL },  // [55] q h7
                { 0x3073105690a6f5a9UL, 0x89d5c6af154cef3bUL },  // [56] q a8
                { 0x22e75265902b0eebUL, 0x1bbd621f8b940f94UL },  // [57] q b8
                { 0xe272538f84561c3eUL, 0xa2952aeba0dff059UL },  // [58] q c8
                { 0x30dffcf181937c03UL, 0xe680d374eae007dbUL },  // [59] q d8
                { 0x5bb82f78aae9d077UL, 0xb9046f0fafe3800aUL },  // [60] q e8
                { 0x7649e8f8d4c59c1cUL, 0xe0ea1934f419ed81UL },  // [61] q f8
                { 0x853c594af551fc83UL, 0x7c92deef2322f674UL },  // [62] q g8
                { 0xcd6859b1d497d3c2UL, 0x20a6997e637ea4adUL },  // [63] q h8
            },
            {
                { 0x4fc7a02561cd1633UL, 0x5d434148e8b793abUL },  // [ 0] k a1
                { 0xe16366a42f37e0b4UL, 0x913e600c2747070aUL },  // [ 1] k b1
                { 0x62d21ce9196199cfUL, 0x4f6970461506a037UL },  // [ 2] k c1
                { 0x2a13768fe2164b4eUL, 0xccb2d5ac3b4b5dbbUL },  // [ 3] k d1
                { 0x63ed7dd22c31104fUL, 0x443484b592bc0ca8UL },  // [ 4] k e1
                { 0x8f91ff20c02cc3bfUL, 0x6b2c4cdd13440bddUL },  // [ 5] k f1
                { 0xffe366471d343473UL, 0xe257aa3215e3ec68UL },  // [ 6] k g1
                { 0xf7f6f9244fcfbbd5UL, 0x5eeaa61387d27c4bUL },  // [ 7] k h1
                { 0x2b5ac5a93c868db3UL, 0x2331c62cef7aa47eUL },  // [ 8] k a2
                { 0xb3f1581cd85df572UL, 0x99a43ff563dcbd11UL },  // [ 9] k b2
                { 0x496e6af8f2174d95UL, 0x1825e034e41aa6aaUL },  // [10] k c2
                { 0xecbb307c3b352ae1UL, 0x12d308eae8fd834aUL },  // [11] k d2
                { 0xbbf237e1a8f22131UL, 0x1891fd783626c364UL },  // [12] k e2
                { 0x88009661b95df026UL, 0x0a1a1aa75b25723eUL },  // [13] k f2
                { 0x56e0a4ee12fd4c39UL, 0x359ceaaeaca9e9cfUL },  // [14] k g2
                { 0xcf653d8399bc2378UL, 0x464e0273c4b8e909UL },  // [15] k h2
                { 0xd21b2c6d42cf0dabUL, 0xe3570f36cd4c41f1UL },  // [16] k a3
                { 0xdcf83700f492778cUL, 0xea6eff52d8a6bf15UL },  // [17] k b3
                { 0x7fa6ff79329032d2UL, 0x8fced09b3149bce2UL },  // [18] k c3
                { 0x9d3aaa3f707db974UL, 0x8a4ad88a5f073585UL },  // [19] k d3
                { 0x9563ce70c827b5e9UL, 0x0f756c58ab9a145aUL },  // [20] k e3
                { 0x13256279b5ca567cUL, 0x48b4d3d09e210984UL },  // [21] k f3
                { 0x58c7f7a97db9cb2fUL, 0xccc995ab20a6f4acUL },  // [22] k g3
                { 0x2a419a13f8c18045UL, 0xf46e42340066bc2dUL },  // [23] k h3
                { 0x6273ce0ba649d7d7UL, 0x4709761676ed6884UL },  // [24] k a4
                { 0x33254ba4d8edb0dcUL, 0x1f37b69c30c5b105UL },  // [25] k b4
                { 0x8f1abe123abf1855UL, 0x9f10b04e89b38608UL },  // [26] k c4
                { 0x099b49daf42a22b8UL, 0x99bfcf5e6962ae17UL },  // [27] k d4
                { 0x5bd9bd87eb93b667UL, 0x0df3152816bbe456UL },  // [28] k e4
                { 0x56fa587aab2f3b63UL, 0x8ce643fc55920000UL },  // [29] k f4
                { 0x15190916ebc649edUL, 0x75455723c21d1faaUL },  // [30] k g4
                { 0x76abbc3fd5cb452dUL, 0x4027732362ee0242UL },  // [31] k h4
                { 0xa5f75e3a4ef6971aUL, 0xe42a59a2c73b36e7UL },  // [32] k a5
                { 0xab55301eb5c00fabUL, 0xacc71e4142eb4566UL },  // [33] k b5
                { 0x75e29eba9a377cd5UL, 0xcf7c387bc2a8c692UL },  // [34] k c5
                { 0x71e055de6bd7ed11UL, 0xca5b32ab12b923c1UL },  // [35] k d5
                { 0x7f8f9b8839fc1709UL, 0xd50392ee13193bbeUL },  // [36] k e5
                { 0xacddb9fbd7c14ab3UL, 0x785cf0b135d95671UL },  // [37] k f5
                { 0x70375a1aacf12459UL, 0x3bfa42a7741de326UL },  // [38] k g5
                { 0x3bec7a4fb137a527UL, 0xd8d6ccca6833a9cbUL },  // [39] k h5
                { 0xd543c4d1f79c1619UL, 0xa433694e573811ddUL },  // [40] k a6
                { 0x3935ecea794aa231UL, 0x17d9b99b114f929dUL },  // [41] k b6
                { 0xc1a20bb3153936bcUL, 0x6036fe533366db0dUL },  // [42] k c6
                { 0x023fab5709b84dacUL, 0xec9ac0803287c8c5UL },  // [43] k d6
                { 0x822b2f3c51203645UL, 0x401286c340a90587UL },  // [44] k e6
                { 0x1a67207d6ff34976UL, 0xd28280d4ec552640UL },  // [45] k f6
                { 0x67e1dcfe236ee73aUL, 0xd554e6ab16b65df2UL },  // [46] k g6
                { 0xf67071d992d6e64dUL, 0xfe3840c0286c1388UL },  // [47] k h6
                { 0x826f9594027cc460UL, 0x056b4573b5597eabUL },  // [48] k a7
                { 0x54cd76fffbc21fd8UL, 0x7d7b3058c188f36aUL },  // [49] k b7
                { 0x111475a9e823ee5aUL, 0x625bc83d864a11b8UL },  // [50] k c7
                { 0xe3a1f780ece40282UL, 0xa5f6e23d6e375ef8UL },  // [51] k d7
                { 0xc3627bfcf00b076fUL, 0xa74fb69950947f10UL },  // [52] k e7
                { 0x2c42368f4a4702f3UL, 0x22286bcc484704f9UL },  // [53] k f7
                { 0x49f860cac71a1c2fUL, 0x25b0286135a92967UL },  // [54] k g7
                { 0xbb836048c68d8e11UL, 0x8ac560711d015389UL },  // [55] k h7
                { 0x4aee3a82f2e85649UL, 0x35ea3bff03003411UL },  // [56] k a8
                { 0xf9493181cb4944adUL, 0xaa8ad2edf64f8520UL },  // [57] k b8
                { 0xdfb4a3c1d8bff4ceUL, 0xd585f0f40bb3dde4UL },  // [58] k c8
                { 0x76741bd6d9544cd2UL, 0xe024127da404fcbbUL },  // [59] k d8
                { 0xe7e8480949e168baUL, 0xaddd78f23b48f927UL },  // [60] k e8
                { 0xb385188b9ff04234UL, 0xd5f3644d39510f15UL },  // [61] k f8
                { 0x654ac1152c33ffb2UL, 0x11ca9c181bc4dcb9UL },  // [62] k g8
                { 0x1414a9256e6b4f52UL, 0x70ff9ec3a709d781UL },  // [63] k h8
            },
        };
    }
}

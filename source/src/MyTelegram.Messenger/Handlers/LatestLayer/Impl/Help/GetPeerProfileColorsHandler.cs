namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Help;

///<summary>
/// Get the set of <a href="https://corefork.telegram.org/api/colors">accent color palettes »</a> that can be used in profile page backgrounds.
/// See <a href="https://corefork.telegram.org/method/help.getPeerProfileColors" />
///</summary>
internal sealed class GetPeerProfileColorsHandler : RpcResultObjectHandler<MyTelegram.Schema.Help.RequestGetPeerProfileColors, MyTelegram.Schema.Help.IPeerColors>,
    Help.IGetPeerProfileColorsHandler
{
    protected override Task<MyTelegram.Schema.Help.IPeerColors> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Help.RequestGetPeerProfileColors obj)
    {
        var colors = new List<TPeerColorOption>
        {
            new() {ColorId = 5},
            new() {ColorId = 3},
            new() {ColorId = 1},
            new() {ColorId = 0},
            new() {ColorId = 2},
            new() {ColorId = 4},
            new() {ColorId = 6},

            new()
            {
                ColorId = 12,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        3379668,
                        8246256
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        5423103,
                        742548
                    ]
                }
            },
            new()
            {
                ColorId = 10,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        2599184,
                        11000919
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        11004782,
                        1474093
                    ]
                }
            },

            new()
            {
                ColorId = 8,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        14712875,
                        16434484
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        15511630,
                        12801812
                    ]
                }
            },

            new()
            {
                ColorId = 7,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        14766162,
                        16363107
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        16749440,
                        10039095
                    ]
                }
            },

            new()
            {
                ColorId = 9,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        10510323,
                        16027647
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        13015039,
                        6173128
                    ]
                }
            },

            new()
            {
                ColorId = 11,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        2600142,
                        8579286
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        4249808,
                        285823
                    ]
                }
            },

            new()
            {
                ColorId = 13,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        14500721,
                        16760479
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        16746150,
                        9320046
                    ]
                }
            },

            new()
            {
                ColorId = 14,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        2391021,
                        15747158,
                        16777215
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        4170494,
                        15024719,
                        16777215
                    ]
                }
            },

            new()
            {
                ColorId = 15,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        14055202,
                        2007057,
                        16777215
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        16748638,
                        3319079,
                        16777215
                    ]
                }
            },

            new()
            {
                ColorId = 16,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        1547842,
                        15223359,
                        16777215
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        6738788,
                        13976655,
                        16777215
                    ]
                }
            },

            new()
            {
                ColorId = 17,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        2659503,
                        7324758,
                        16777215
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        2276578,
                        4039232,
                        16777215
                    ]
                }
            },

            new()
            {
                ColorId = 18,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        826035,
                        16756117,
                        16770741
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        2276578,
                        16750456,
                        16767595
                    ]
                }
            },

            new()
            {
                ColorId = 19,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        7821270,
                        16225808,
                        16768654
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        9933311,
                        15889181,
                        16767833
                    ]
                }
            },

            new()
            {
                ColorId = 20,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        1410511,
                        15903517,
                        16777215
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        4040427,
                        15639837,
                        16777215
                    ]
                }
            },

            new()
            {
                ColorId = 21,
                Colors = new TPeerColorSet
                {
                    Colors =
                    [
                        0xfc4528,
                        0x4fd57e,
                        0xf62e7b
                    ]
                },
                DarkColors = new TPeerColorSet
                {
                    Colors =
                    [
                        0x640db5,
                        0x498141,
                        0xbf5103
                    ]
                }
            },

        };

        return Task.FromResult<MyTelegram.Schema.Help.IPeerColors>(new TPeerColors
        {
            Colors = [.. colors]
        });
    }
}

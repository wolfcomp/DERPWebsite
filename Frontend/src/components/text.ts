import { DateTime } from "luxon"

const debug = false;

const strings: { [id: string]: { [id: string]: string[] } } = {
    home: {
        norm: [
            `Welcome one and all to Dynamis Extreme Raiding Parties, aka DERP!`,
            `This is a high end focused Dynamis server with the goal to ensure a safe learning environment for those wanting to get into harder content. Here you will find parties hosted by both our team and other players within Ultimates, Savage, Extremes, Unreals, Criterion, BLU and more!`,
            `We wish to become a hub for Dynamis players to find others to raid with, regardless of if they are newer to the game or veterans.`
        ],
        owod: [
            `Wewcome one and aww to Dynamis Extweme W-Waiding Pawties, ^•ﻌ•^ a-aka DEWP!`,
            `This is a high end focused Dynamis s-sewvew with the g-goaw to ensuwe a-a safe weawning e-enviwonment fow t-those wanting to g-get into hawdew c-content. :3 hewe y-you wiww find pawties hosted by both ouw team and othew pwayews within Uwtimates, (U ﹏ U) S-savage, Extwemes, -.- Unweaws, Cwitewion, (ˆ ﻌ ˆ)♡ bwu and m-mowe!`,
            `We wish to become a hub fow Dynamis p-pwayews to find o-othews to waid w-with, o.O wegawdwess o-of if they awe n-nyewew to the g-game ow vetewans.`
        ]
    },
    join: {
        norm: [
            `Join our Discord`
        ],
        owod: [
            `Join ouw Discowd`
        ]
    },
    nav: {
        norm: [
            'Home',
            'About',
            'Schedule',
            'Resources',
            'Slideshow',
            'Files'
        ],
        owod: [
            'Home',
            'About',
            'Scheduwe',
            'Wesouwces',
            'Swideshow',
            'Fiwes'
        ]
    },
    blur: {
        norm: [
            'Blur background'
        ],
        owod: [
            'Bwuw backgwound'
        ]
    },
    login: {
        norm: [
            'Login'
        ],
        owod: [
            'Wogin'
        ]
    },
    title: {
        norm: [
            'DERP'
        ],
        owod: [
            'DEWP'
        ]
    },
    slideshow: {
        norm: [
            'A Realm Reborn',
            'Heavensward',
            'Stormblood',
            'Shadowbringers',
            'Endwalker',
            'Dawntrail'
        ],
        owod: [
            'A Weawm Webown',
            'Heavenswawd',
            'Stowmbwood',
            'Shadowbwingews',
            'Endwawkew',
            'Dwantwaiw'
        ]
    },
    footer: {
        norm: [
            '© 2024 WildWolf'
        ],
        owod: [
            '© 2024 WildWolf ÛwÛ I hope you have a wondewfuw day! ÛwÛ'
        ]
    },
    slideshowToggle: {
        norm: [
            'Start auto shift',
            'Stop auto shift'
        ],
        owod: [
            'Stawt auto shift',
            'Stop auto shift'
        ]
    }
}

export default function getString(id: string, time?: DateTime) {
    if (!time) time = DateTime.local();
    if (debug) return strings[id].owod;
    return strings[id][time.month === 4 && time.day === 1 ? "owod" : "norm"];
}
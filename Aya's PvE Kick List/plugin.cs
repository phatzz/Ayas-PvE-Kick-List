using System.Linq;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO; // Used for Licensing 
using AimsharpWow.API; //needed to access Aimsharp API 
using System.Net;
using System.Management;
namespace AimsharpWow.Modules
{
    public class PvEKicks : Plugin
    {
        bool authorized = true;
        public class Enemy
        {
            public string Unit = "";
            public int HP = 0;
            public bool IsInterruptable = false;
            public bool IsChanneling = false;
            public int CastingID = 0;
            public int CastingRemaining = 0;
            public int CastingElapsed = 0;
            public int Range = 0;
            public string Spec = "none";
            public Enemy(string unit)
            {
                Unit = unit;
            }
            public void Update()
            {
                HP = Aimsharp.Health(Unit);
                if (HP == 0)
                {
                    IsInterruptable = false;
                    IsChanneling = false;
                    CastingID = 0;
                    CastingRemaining = 0;
                    CastingElapsed = 0;
                    Range = 0;
                    Spec = "none";
                }
                else
                {
                    IsInterruptable = Aimsharp.IsInterruptable(Unit);
                    IsChanneling = Aimsharp.IsChanneling(Unit);
                    CastingID = Aimsharp.CastingID(Unit);
                    CastingRemaining = Aimsharp.CastingRemaining(Unit);
                    CastingElapsed = Aimsharp.CastingElapsed(Unit);
                    Range = Aimsharp.Range(Unit);
                    Spec = Aimsharp.GetSpec(Unit);
                }
            }
        }
        string[] immunes = { "Divine Shield", "Aspect of the Turtle" };
        string[] physical_immunes = { "Blessing of Protection" };
        string[] spell_immunes = { "Nether Ward", "Grounding Totem Effect", "Spell Reflection", "Anti-Magic Shell" };
        Random rng = new Random();
        Stopwatch RngTimer = new Stopwatch();
        Stopwatch LassoTimer = new Stopwatch();
        public override void LoadSettings()
        {
            Settings.Add(new Setting("Kick at milliseconds remaining", 100, 1500, 1000));
            Settings.Add(new Setting("Kick channels after milliseconds", 50, 2000, 500));
            Settings.Add(new Setting("Minimum delay", 50, 2000, 500));
            List<string> ClassList = new List<string>(new string[] { "Shaman", "Death Knight", "Guardian Druid", "Monk", "Mage", "Hunter", "Shadow Priest", "Rogue", "Demon Hunter", "Warrior", "Paladin", "Warlock" });
            Settings.Add(new Setting("Class", ClassList, "Monk"));
            Settings.Add(new Setting("Kick from OoC?", true));
            Settings.Add(new Setting("Use CC to interrupt?", true));
            Settings.Add(new Setting("Kick Mousover targets?", true));
            Settings.Add(new Setting("Use Raid Kick list?", false));

        }
        List<string> Interrupts = new List<string>();
        List<string> CCInterrupts = new List<string>();
        string Class = "";
        bool KickArena = true;
        bool OOCKicks = true;
        bool UseCC = true;
        int KickValue = 0;
        int KickChannelsAfter = 0;
        int MinimumDelay = 0;
        Enemy Target = new Enemy("target");
        Enemy Focus = new Enemy("focus");
        List<Enemy> Enemies = new List<Enemy>();
        public override void Initialize()
        {
            if (authorized)
            {
            }
            Class = GetDropDown("Class");
            OOCKicks = GetCheckBox("Kick from OoC?");
            UseCC = GetCheckBox("Use CC to interrupt?");
            Enemies.Add(Target);
            Enemies.Add(Focus);
            Aimsharp.PrintMessage("Aya's M+/Raid Kicks Plugin");
            Aimsharp.PrintMessage("Automated kicking for your target and focus");
            Aimsharp.PrintMessage("Do not use this together with any other Kicks plugin!");
            Aimsharp.PrintMessage("Use this macro to hold your kicks for a number of seconds: /xxxxx SaveKicks #");
            Aimsharp.PrintMessage("For example: /xxxxx SaveKicks 5");
            Aimsharp.PrintMessage("will make the bot not kick anything for the next 5 seconds.");
            if (Class == "Hunter")
            {
                Interrupts.Add("Counter Shot");
                Interrupts.Add("Muzzle");
                if (UseCC)
                {
                    CCInterrupts.Add("Intimidation");
                }
            }
            if (Class == "Rogue")
            {
                Interrupts.Add("Kick");
            }
            if (Class == "Shadow Priest")
            {
                Interrupts.Add("Silence");
            }
            if (Class == "Demon Hunter")
            {
                Interrupts.Add("Disrupt");
            }
            if (Class == "Shaman")
            {
                Interrupts.Add("Wind Shear");
                if (UseCC)
                {
                    CCInterrupts.Add("Capacitor Totem");
                }
            }
            if (Class == "Paladin")
            {
                Interrupts.Add("Rebuke");
            }
            if (Class == "Death Knight")
            {
                Interrupts.Add("Mind Freeze");
                if (UseCC)
                {
                    CCInterrupts.Add("Asphyxiate");
                    CCInterrupts.Add("Death Grip");
                }
            }
            if (Class == "Guardian Druid")
            {
                Interrupts.Add("Skull Bash");
                if (UseCC)
                {
                    CCInterrupts.Add("Mighty Bash");
                }
            }
            if (Class == "Warlock")
            {
                Interrupts.Add("Spell Lock");
            }
            if (Class == "Mage")
            {
                Interrupts.Add("Counterspell");
            }
            if (Class == "Monk")
            {
                Interrupts.Add("Spear Hand Strike");
                if (UseCC)
                {
                    CCInterrupts.Add("Paralysis");
                    CCInterrupts.Add("Leg Sweep");
                }
            }
            if (Class == "Warrior")
            {
                Interrupts.Add("Pummel");
            }
            foreach (string Interrupt in Interrupts)
            {
                Spellbook.Add(Interrupt);
                Macros.Add(Interrupt + "focus", "/cast [@focus] " + Interrupt);
                Macros.Add(Interrupt + "mouseover", "/cast [@mouseover] " + Interrupt);
            }
            foreach (string CCInterrupt in CCInterrupts)
            {
                Spellbook.Add(CCInterrupt);
                Macros.Add(CCInterrupt + "focus", "/cast [@focus] " + CCInterrupt);
                Macros.Add(CCInterrupt + "mouseover", "/cast [@mouseover] " + CCInterrupt);
            }
            foreach (string immune in immunes)
            {
                Buffs.Add(immune);
            }
            foreach (string spell_immune in spell_immunes)
            {
                Buffs.Add(spell_immune);
            }
            foreach (string physical_immune in physical_immunes)
            {
                Buffs.Add(physical_immune);
            }
            string SpellsToKickNormRaid = "{360176, 366392, 364030, 360259, 362383, 361913, 350342, 350286, 350283, 351779, 357144, 348428, 352141, 355540, 325590, 337110, 325590, 328254, 333002, 337110}";
            string SpellsToKickCCRaid = "{365008, 364073}";
            string SpellsToKickNormMplus = "{332707, 332666, 332706, 332612, 332084, 321764, 320008, 332608, 328740, 323064, 332605, 328707, 333875, 334076, 332196, 331379, 332234, 332705, 325700, 326607, 323552, 323538, 325876," +
                                            "338003, 328322, 322938, 324914, 324776, 326046, 340544, 337235, 337251, 337253, 322450, 321828, 322767, 331743, 323057, 335143, 334748, 330784, 324293, 338353, 323190, 327130, 328667," +
                                            "320571, 340210, 328180, 321999, 328094, 328016, 329239, 329917, 327995, 328002,328094, 322358, 328534, 328475, 319654, 322433, 321038, 334653, 335305, 336277, 326952, 326836, 326712," +
                                            "326837, 320861, 321105, 327413, 317936, 317963, 328295, 328137, 328331, 327648, 317959, 327481, 317661, 341902, 330784, 333231, 320300, 320120, 341969, 330703, 342139, 330562, 330810," +
                                            "330868, 341771, 330875, 342675, 323190, 263085, 294526, 330438, 297018, 252057, 252063, 328869, 297310, 330477, 332165, 329930, 294517, 296839, 294165, 330118, 183345, 297024, 258935," +
                                            "277040, 242391, 330573, 326399, 345554, 327461, 330479, 310392, 184381, 334538, 329322, 330755, 330822, 295929, 318995, 167012, 354493, 352215, 304946, 242733, 355888, 355930, 355934," +
                                            "354297, 356324, 356404, 356407, 355641, 353835, 347775, 347903, 350922, 357188, 354297, 355225, 355234, 357284, 357260, 351119, 352347, 356843, 355737, 358967, 366566}";
            string SpellsToKickCCMplus = "{332329, 332671, 332156, 334664, 326450, 325701, 331743, 322569, 324987, 325021, 320822, 321807, 321780, 320822, 334747, 338022, 328400, 328177, 336451, 328429, 328338, 329163, 321935, 324609," +
                                            "330586, 333540, 330532, 330694, 295985, 335528, 330822, 304254, 332181, 297966, 358328, 241687, 355915, 356031, 355057, 355132}";



            string InRangeItem = "0";
            if (Class == "Monk" || Class == "Guardian Druid" || Class == "Death Knight" || Class == "Rogue" || Class == "Demon Hunter" || Class == "Warrior" || Class == "Paladin")
            {
                InRangeItem = "32321";
            }
            if (Class == "Mage" || Class == "Shaman" || Class == "Hunter" || Class == "Shadow Priest" || Class == "Warlock")
            {
                InRangeItem = "18904";
            }
            string MOCastRemaining = GetSlider("Kick at milliseconds remaining").ToString();
            string MOChannelRemaining = GetSlider("Kick channels after milliseconds").ToString();
            CustomFunctions.Add("ShouldKickMONormMplus",
                "local ShouldKickMO = 0" + "\n local WhiteList = " + SpellsToKickNormMplus +
                "\nif UnitExists(\"mouseover\") then\n" +
                    "local CAname, CAtext, CAtexture, CAstartTimeMS, CAendTimeMS, CAisTradeSkill, CAcastID, CAnotInterruptible, CAspellId = UnitCastingInfo(\"mouseover\")\n" +
                    "local name, text, texture, startTimeMS, endTimeMS, isTradeSkill, notInterruptible, spellId = UnitChannelInfo(\"mouseover\")\n" +
                    "if IsItemInRange(" + InRangeItem + ", \"mouseover\") and UnitCanAttack(\"player\", \"mouseover\") == true and UnitIsDead(\"mouseover\") ~= true then\n" +
                        "if CAspellID ~= nil or spellID ~= 0 then\n" +
                            "for _, k in pairs(WhiteList) do\n" +
                                "if k == CAspellId then\n" +
                                    "if CAendTimeMS - CAstartTimeMS >= " + MOCastRemaining + " and CAnotInterruptible == false then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "else if k == spellID then\n" +
                                    "if (GetTime() * 1000) - startTimeMS >= " + MOChannelRemaining + " and CAnotInterruptible == false then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "end\n" +
                            "end\n" +
                        "end\n" +
                    "end\n" +
                "end\n" +
            "end\n" +
            "return ShouldKickMO");
            CustomFunctions.Add("ShouldKickMOCCMplus",
                "local ShouldKickMO = 0" + "\n local WhiteList = " + SpellsToKickCCMplus +
                "\nif UnitExists(\"mouseover\") then\n" +
                    "local CAname, CAtext, CAtexture, CAstartTimeMS, CAendTimeMS, CAisTradeSkill, CAcastID, CAnotInterruptible, CAspellId = UnitCastingInfo(\"mouseover\")\n" +
                    "local name, text, texture, startTimeMS, endTimeMS, isTradeSkill, notInterruptible, spellId = UnitChannelInfo(\"mouseover\")\n" +
                    "if IsItemInRange(" + InRangeItem + ", \"mouseover\") and UnitCanAttack(\"player\", \"mouseover\") == true and UnitIsDead(\"mouseover\") ~= true then\n" +
                        "if CAspellID ~= nil or spellID ~= 0 then\n" +
                            "for _, k in pairs(WhiteList) do\n" +
                                "if k == CAspellId then\n" +
                                    "if CAendTimeMS - CAstartTimeMS >= " + MOCastRemaining + " then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "else if k == spellID then\n" +
                                    "if (GetTime() * 1000) - startTimeMS >= " + MOChannelRemaining + " then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "end\n" +
                            "end\n" +
                        "end\n" +
                    "end\n" +
                "end\n" +
            "end\n" +
            "return ShouldKickMO");
            CustomFunctions.Add("ShouldKickMORaid",
                "local ShouldKickMO = 0" + "\n local WhiteList = " + SpellsToKickNormRaid +
                "\nif UnitExists(\"mouseover\") then\n" +
                    "local CAname, CAtext, CAtexture, CAstartTimeMS, CAendTimeMS, CAisTradeSkill, CAcastID, CAnotInterruptible, CAspellId = UnitCastingInfo(\"mouseover\")\n" +
                    "local name, text, texture, startTimeMS, endTimeMS, isTradeSkill, notInterruptible, spellId = UnitChannelInfo(\"mouseover\")\n" +
                    "if IsItemInRange(" + InRangeItem + ", \"mouseover\") and UnitCanAttack(\"player\", \"mouseover\") == true and UnitIsDead(\"mouseover\") ~= true then\n" +
                        "if CAspellID ~= nil or spellID ~= 0 then\n" +
                            "for _, k in pairs(WhiteList) do\n" +
                                "if k == CAspellId then\n" +
                                    "if CAendTimeMS - CAstartTimeMS >= " + MOCastRemaining + " and CAnotInterruptible == false then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "else if k == spellID then\n" +
                                    "if (GetTime() * 1000) - startTimeMS >= " + MOChannelRemaining + " and CAnotInterruptible == false then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "end\n" +
                            "end\n" +
                        "end\n" +
                    "end\n" +
                "end\n" +
            "end\n" +
            "return ShouldKickMO");
            CustomFunctions.Add("ShouldKickMOCCRaid",
                "local ShouldKickMO = 0" + "\n local WhiteList = " + SpellsToKickCCRaid +
                "\nif UnitExists(\"mouseover\") then\n" +
                    "local CAname, CAtext, CAtexture, CAstartTimeMS, CAendTimeMS, CAisTradeSkill, CAcastID, CAnotInterruptible, CAspellId = UnitCastingInfo(\"mouseover\")\n" +
                    "local name, text, texture, startTimeMS, endTimeMS, isTradeSkill, notInterruptible, spellId = UnitChannelInfo(\"mouseover\")\n" +
                    "if IsItemInRange(" + InRangeItem + ", \"mouseover\") and UnitCanAttack(\"player\", \"mouseover\") == true and UnitIsDead(\"mouseover\") ~= true then\n" +
                        "if CAspellID ~= nil or spellID ~= 0 then\n" +
                            "for _, k in pairs(WhiteList) do\n" +
                                "if k == CAspellId then\n" +
                                    "if CAendTimeMS - CAstartTimeMS >= " + MOCastRemaining + " then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "else if k == spellID then\n" +
                                    "if (GetTime() * 1000) - startTimeMS >= " + MOChannelRemaining + " then\n" +
                                        "ShouldKickMO = 1\n" +
                                    "end\n" +
                                "end\n" +
                            "end\n" +
                        "end\n" +
                    "end\n" +
                "end\n" +
            "end\n" +
            "return ShouldKickMO");
            CustomCommands.Add("SaveKicks");
        }
        int[] Raid =
        { 
        //Sepulcher of the First Ones 
        360176, //Blast 
        366392, //Searing Ablation 
        364030, //Debilitating Ray 
        360259, //Gloom Bolt 
        362383, //Anima Bolt 
        365008, //Psychic Terror (CC to interrupt) 
        361913, //Manifest Shadows 
        364073, //Degenerate (CC to interrupt) 
        //Sanctum of Domination 
        350342, //Formless Mass 
        350286, //Song of Dissolution 
        350283, //Soulful Blast 
        351779, //Agonizing Nova 
        357144, //Despair 
        348428, //Piercing Wall 
        352141, //Banshees Cry 
        355540, //Ruin 
        325590, //Sunking 
        337110, //Council Frida 
        325590, //Scorful Blast 
        328254, //Shattering Ruby 
      	333002, //Vulgar Brand 
        337110, //Dreadbolt Volley 
        };
        int[] MythicPlus =
        { 
        //De Other Side 
        332329, //Devoted Sacrifice (CC to interrupt) 
        332671, //Bladestorm (CC to interrupt) 
        332707, //Shadow Word Pain 
        332666, //Renew 
        332706, //Heal 
        332612, //Healing Wave 
        332084, //Self-Cleaning Cycle 
        321764, //Bark Armor 
        320008, //Frostbolt 
        332608, //Lightning Discharge 
        328740, //Dark Lotus 
        323064, //Blood Barrage 
        332605, //Hex 
        328707, //Scribe 
        333875, //Deaths Embrace 
        334076, //Shadowcore 
        332196, //Discharge 
        331379, //Lubricate 
        332234, //Essential Oil 
        332705, //Smite 
        332156, //Spinning Up (CC to interrupt) 
        334664, //Frightened Cries (CC to interrupt) 
        //Halls of Attonement 
        326450, //Loyal Beasts (CC to interrupt) 
        325700, //Collect Sins 
        325701, //Siphon Life (CC to interrupt) 
        326607, //Turn to Stone 
        323552, //Volley of Power 
        323538, //Bolt of Power 
        325876, //Curse of Obliteration 
        338003, //Wicked Bolt 
        328322, //Villainous Bolt 
        //Mists of Tirna Scithe 
        322938, //Harvest Essence 
        324914, //Nourish the Forest 
        324776, //Bramblethorn Coat 
        326046, //Stimulate Resistance 
        340544, //Stimulate Regeneration 
        337235, //Parasitic Pacification 
        337251, //Parasitic Incapacitation 
        337253, //Parasitic Domination 
        322450, //Consumption 
        321828, //Patty Cake 
        322767, //Spirit Bolt 
        331743, //Bucking Rampage (CC to interrupt) 
        322569, //Hand of Thros (CC to interrupt) 
        323057, //Spirit Bolt 
        324987, //Mistveil Bite (CC to interrupt) 
        325021, //Mistveil Tear (CC to interrupt) 
        //Necrotic Wake 
        335143, //Bonemend 
        334748, //Drain Fluids 
        330784, //Necrotic Bolt 
        320822, //Final Bargain (CC to interrupt) 
        324293, //Rasping Scream 
        338353, //Goresplatter 
        323190, //Meat Shield 
        327130, //Repair Flesh 
        328667, //Frostbolt Volley 
        321807, //Boneflay (CC to interrupt) 
        321780, //Animate Dead (CC to interrupt) 
        320822, //Final Bargain (CC to interrupt) 
        334747, //Throw Flesh (CC to interrupt) 
        320571, //Shadow Well 
        338022, //Leap (CC to interrupt) 
        //Plaguefall 
        328400, //Stealthlings (CC to interrupt) 
        328177, //Fungistorm ( CC to interrupt) 
        336451, //Bulwark of Maldraxxus (CC to interrupt) 
        328429, //Crushing Embrace (CC to interrupt) 
        340210, //Corrosive Gunk 
        328180, //Gripping Infection 
        321999, //Viral Globs 
        328094, //Pestilence Bolt 
        328016, //Wonder Grow 
        328338, //Call Venomfang (CC to interrupt) 
        329239, //Creepy Crawlers 
        329917, //Binding Fungus 
        327995, //Doom Shroom 
        328002, //Hurl Spores 
        328094, //Pestilence Bolt 
        322358, //Burning Strain 
        328534, //Vile Spit 
        328475, //Enveloping Webbing 
        329163, //Ambush (CC to interrupt) 
        321935, //Withering Filth (CC to interrupt) 
        //Sanguine Depths 
        319654, //Hungering Drain 
        322433, //Stoneskin 
        321038, //Wrack Soul 
        334653, //Engorge 
        335305, //Barbed Shackles 
        336277, //Explosive Anger 
        326952, //Fiery Cantrip 
        326836, //Curse of Suppression 
        326712, //Dark Bolt 
        326837, //Gloom Burst 
        324609, //Animate Weapon (CC to interrupt) 
        320861, //Drain Essence 
        321105, //Sap Lifeblood 
        //Growing Mistrust (CC to interrupt) 
        //Spires of Ascension  
        327413, //Rebellious Fist 
        317936, //Forsworn Doctrine 
        317963, //Burden of Knowledge 
        328295, //Greater Mending 
        328137, //Dark Pulse 
        328331, //Forced Confession 
        327648, //Internal Strife 
        317959, //Dark Lash 
        327481, //Dark Lance 
        317661, //Insidious Venom 
        //Theater of Pain 
        341902, //Unholy Fervor 
        330784, //Necrotic Bolt 
        333231, //Searing Death 
        320300, //Necromantic Bolt 
        320120, //Plague Bolt 
        341969, //Withering Discharge 
        330703, //Decaying Filth 
        342139, //Battle Trance 
        330562, //Demoralizing Shout 
        330810, //Bind Soul 
        330868, //Necrotic Bolt Volley 
        341771, //Grave Spike 
        330875, //Spirit Frost 
        342675, //Bone Spear 
        330586, //Devour Flesh (CC to interrupt) 
        323190, //Meat Shield 
        333540, //Opportunity Strikes (CC to interrupt) 
        330532, //Jagged Quarrel (CC to interrupt) 
        330694, //Leaping Thrash (CC to interrupt) 
        //Torgast 
        263085, //Terrifying Roar 
        294526, //Curse of Frailty 
        330438, //Fearsome Howl 
        297018, //Fearsome Howl 
        252057, //Cripping Burst 
        252063, //Dread Plague 
        328869, //Dark Bolt Volley 
        297310, //Steal Vitality 
        330477, //Prophecy of Death 
        332165, //Fearsome Shriek 
        329930, //Terrifying Screeh 
        294517, //Phasing Roar 
        296839, //Dearth Blast 
        294165, //Accursed Strength 
        330118, //Withering Roar 
        295985, //Ground Crush (CC to interrupt) 
        335528, //Inferno (CC to interrupt) 
        183345, //Shadow Bolt 
        297024, //Soul Echo 
        258935, //Inner Flames 
        329423, //Inner Flames 2
        258938, //Inner Flames 3
        277040, //Soul of Mist 
        242391, //Terror 
        330573, //Bounty of The Forest 
        326399, //Crush 
        345554, //Stygian Shield 
        327461, //Meat Hook 
        330479, //Gunk 
        310392, //Intimidating Presence 
        184381, //Interrupting Slam 
        334538, //Deaden Magic 
        329322, //Soul Bolt 
        330755, //Focused Blast 
        330822, //Ocular Beam (CC to interrupt) 
        295929, //Rats 
        318995, //Deafening Howl 
        167012, //Incorporeal 
        304254, //Devour Soul (CC to interrupt) 
        332181, //Mass Devour (CC to interrupt) 
        297966, //Devour Obleron Armaments (CC to interrupt) 
        354493, //Soul Breaker 
        352215, //Cries of the Tormented 
        358328, //Tortured Stomp (CC to interrupt) 
        304946, //Shadow Rip 
        //Mage Tower – Hunter 
        242733, //Fel Burst 
        241687, //Sonic Scream (CC to interrupt) 
        //Tazavesh Streets of Wonder 
        355888, //Hard Light Baton 
        355930, //Spark Burn 
        355934, //Hard Light Barrier 
        354297, //Hyperlight Bolt 
        356324, //Empowered Glyph of Restraint 
        355915, //Glyph of Restraint (CC to interrupt) 
        356031, //Stasis Beam (CC to interrupt) 
        356404, //Lava Breath 
        356407, //Ancient Dread 
        355641, //Scintillate 
        353835, //Suppression 
        347775, //Spam Filter 
        347903, //Junk Mail 
        350922, //Menacing Shout 
        357188, //Double Technique 
        //Tazavesh Soleah’s Gambit 
        354297, //Hyper Light Bolt 
        355057, //Cry of Mrrggllrrgg (CC to interrupt) 
        355225, //Water Bolt 
        355234, //Volatile Pufferfish 
        357284, //Reinvigorate 
        357260, //Unstable Rift 
        351119, //Shuriken Blitz 
        352347, //Valorous Bolt 
        355132, //Invigorating Fish Stick (CC to interrupt) 
        356843, //Brackish Bolt 
        //Affixes Season 2 
        355737, //Scorching Blast 
        358967, //Inferno 
        //Affixes Season 3 
        366566, //Burst 
        };
        int RandomDelay = 0;
        public override bool CombatTick()
        {
            if (authorized)
            {
                foreach (Enemy t in Enemies)
                {
                    t.Update();
                }
                if (!RngTimer.IsRunning)
                {
                    RngTimer.Restart();
                    RandomDelay = rng.Next(0, 500);
                }
                if (RngTimer.ElapsedMilliseconds > 10000)
                {
                    RngTimer.Restart();
                    RandomDelay = rng.Next(0, 500);
                }
                KickValue = GetSlider("Kick at milliseconds remaining");
                KickChannelsAfter = GetSlider("Kick channels after milliseconds");
                MinimumDelay = GetSlider("Minimum delay");
                bool IAmChanneling = Aimsharp.IsChanneling("player");
                int GCD = Aimsharp.GCD();
                float Haste = Aimsharp.Haste() / 100f;
                bool LineOfSighted = Aimsharp.LineOfSighted();
                bool NoKicks = Aimsharp.IsCustomCodeOn("SaveKicks");
                bool InRange = false;
                bool ShouldKickMOMplus = false;
                bool ShouldKickMOCCMplus = false;
                bool ShouldKickMORaid = false;
                bool ShouldKickMOCCRaid = false;

                if (GetCheckBox("Kick Mousover targets?"))
                {
                    ShouldKickMOMplus = Aimsharp.CustomFunction("ShouldKickMONormMplus") == 1;
                    if (GetCheckBox("Use CC to interrupt?"))
                    {
                        ShouldKickMOMplus = Aimsharp.CustomFunction("ShouldKickMOCCMplus") == 1;
                        if (GetCheckBox("Use Raid Kick list?"))
                        {
                            ShouldKickMOCCRaid = Aimsharp.CustomFunction("ShouldKickMOCCRaid") == 1;
                        }
                    }
                    if (GetCheckBox("Use Raid Kick list?"))
                    {
                        ShouldKickMORaid = Aimsharp.CustomFunction("ShouldKickMORaid") == 1;
                    }
                }

                if (Class == "Monk" || Class == "Guardian Druid" || Class == "Death Knight" || Class == "Rogue" || Class == "Demon Hunter" || Class == "Warrior" || Class == "Paladin")
                {
                    InRange = Aimsharp.Range("target") <= 10;
                }
                if (Class == "Mage" || Class == "Shaman" || Class == "Hunter" || Class == "Shadow Priest")
                {
                    InRange = Aimsharp.Range("target") <= 35;
                }
                KickValue = KickValue + RandomDelay;
                KickChannelsAfter = KickChannelsAfter + RandomDelay;
                if (IAmChanneling || LineOfSighted)
                    return false;
                if (!NoKicks)
                {
                    foreach (string Interrupt in Interrupts)
                    {
                        foreach (Enemy t in Enemies)
                        {
                            if (Aimsharp.CanCast(Interrupt, "target") && InRange)
                            {
                                //Raid list
                                if ((GetCheckBox("Use Raid Kick list?")) && (Raid.Contains(t.CastingID) && (!t.IsChanneling && t.CastingRemaining < KickValue || t.IsChanneling && t.CastingElapsed > KickChannelsAfter) && t.IsInterruptable && t.CastingElapsed > MinimumDelay))
                                {
                                    if (t.Unit == "target")
                                    {
                                        Aimsharp.Cast(Interrupt, true);
                                        return true;
                                    }
                                    else
                                    {
                                        Aimsharp.Cast(Interrupt + t.Unit, true);
                                        return true;
                                    }
                                }
                                //M+ list
                                if (MythicPlus.Contains(t.CastingID) && (!t.IsChanneling && t.CastingRemaining < KickValue || t.IsChanneling && t.CastingElapsed > KickChannelsAfter) && t.IsInterruptable && t.CastingElapsed > MinimumDelay)
                                {
                                    if (t.Unit == "target")
                                    {
                                        Aimsharp.Cast(Interrupt, true);
                                        return true;
                                    }
                                    else
                                    {
                                        Aimsharp.Cast(Interrupt + t.Unit, true);
                                        return true;
                                    }
                                }
                            }
                        }
                        if (ShouldKickMOMplus && Aimsharp.CanCast(Interrupt, "target"))
                        {
                            Aimsharp.Cast(Interrupt + "mouseover", true);
                            return true;
                        }
                        if (ShouldKickMORaid && Aimsharp.CanCast(Interrupt, "target"))
                        {
                            Aimsharp.Cast(Interrupt + "mouseover", true);
                            return true;
                        }
                    }
                    foreach (string CCInterrupt in CCInterrupts)
                    {
                        foreach (Enemy t in Enemies)
                        {
                            if (Aimsharp.CanCast(CCInterrupt, "target") && InRange)
                            {
                                //kick raid spells
                                if ((GetCheckBox("Use Raid Kick list?")) && (Raid.Contains(t.CastingID) && (!t.IsChanneling && t.CastingRemaining < KickValue || t.IsChanneling && t.CastingElapsed > KickChannelsAfter) && t.CastingElapsed > MinimumDelay))
                                {
                                    if (t.Unit == "target")
                                    {
                                        Aimsharp.Cast(CCInterrupt, true);
                                        return true;
                                    }
                                    else
                                    {
                                        Aimsharp.Cast(CCInterrupt + t.Unit, true);
                                        return true;
                                    }
                                }
                                //kick mplus spells
                                if (MythicPlus.Contains(t.CastingID) && (!t.IsChanneling && t.CastingRemaining < KickValue || t.IsChanneling && t.CastingElapsed > KickChannelsAfter) && t.CastingElapsed > MinimumDelay)
                                {
                                    if (t.Unit == "target")
                                    {
                                        Aimsharp.Cast(CCInterrupt, true);
                                        return true;
                                    }
                                    else
                                    {
                                        Aimsharp.Cast(CCInterrupt + t.Unit, true);
                                        return true;
                                    }
                                }
                            }
                        }
                        if (ShouldKickMOCCMplus && Aimsharp.CanCast(CCInterrupt, "target"))
                        {
                            Aimsharp.Cast(CCInterrupt + "mouseover", true);
                            return true;
                        }
                        if (ShouldKickMOCCRaid && Aimsharp.CanCast(CCInterrupt, "target"))
                        {
                            Aimsharp.Cast(CCInterrupt + "mouseover", true);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public override bool OutOfCombatTick()
        {
            if (authorized)
            {
                if (OOCKicks)
                    return CombatTick();
            }
            return false;
        }
    }
}

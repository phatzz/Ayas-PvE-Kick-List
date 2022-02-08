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
            List<string> ClassList = new List<string>(new string[] { "Shaman", "Death Knight", "Guardian Druid", "Monk", "Mage", "Hunter", "Shadow Priest", "Rogue", "Demon Hunter", "Warrior","Paladin", "Warlock" });
            Settings.Add(new Setting("Class", ClassList, "Monk"));
            Settings.Add(new Setting("Kick from OoC?", true));
            Settings.Add(new Setting("Use CC to interrupt?", true));
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

            string SpellsToKick = "{325590, 333002, 328254, 337110, 325590, 328254, 333002, 337110, 332329,	332671,	331927,	340026,	332666,	332706,	332612,	332084,	321764,	320008,	332608,	328729,	323064,	332605, 326450,	325523,	325700,	325701," +
                "326607, 323552, 323538,326021,	331718,	331743,	322486,	322938,	324914,	324776,	326046,	340544,	337235,	337251,	337253,	322450,	322527,	321828,	335143,	334748,	320462,	324293,	320170,	338353,	323190,	327130,	322493,	328400," +
                "318949,328177,	330403,	336451,	328429,	319070,	328180,	321999,	328094,	328016,	328338,	324609,	335305,	319654,	322433,	321038,	334653,	335305,	336277,	326952,	326836, 327413, 317936, 317963,	328295,	328137,	328331,	341902," +
                "341969,342139,	330562,	330810,	330868,	341771,	330532,	330875,	319669,	324589,	342675,	330586,	263085, 294526, 298844, 332165, 329930, 294517, 296839, 294165, 330118, 258935, 277040, 242391, 330573, 345554, 327461, 330479," +
                "310392, 184381, 334538, 330755, 295929, 332181, 297966, 355930, 355934, 354297, 356537, 347775, 355057, 355225, 347903, 358131, 350922, 357404, 156877, 347152, 351119, 346980, 352347, 355132, 355737, 358967}";

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

            CustomFunctions.Add("ShouldKickMO",
                "local ShouldKickMO = 0" + "\n local WhiteList = " + SpellsToKick +
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

            CustomCommands.Add("SaveKicks");
        }

        int[] Raid =
        {
            325590, //sunking
            333002,
            328254,
            337110, //council Frida
            325590, //scorful Blast
            328254, //Shattering Ruby
            333002, //Vulgar Brand
            337110, //Dreadbolt Volley
        };

        int[] MythicPlus =
        {
            // De OtherSide
			332329,	//Devoted Sacrifice
			332671,	//Bladestorm
            331927,	//Haywire
			340026,	//Wailing Grief	
			332666,	//Renew
            332706,	//Heal
            332612,	//Healing Wave
            332084,	//Self-Cleaning Cycle
            321764,	//Bark Armor
            320008,	//Frostbolt
            332608,	//Lightning Discharge
            328729,	//Dark Lotus
            323064,	//Blood Barrage
            332605,	//Hex
			// Halls of Attonement
            326450,	//Loyal Beasts
            325523,	//Deadly Thrust
            325700,	//Collect Sins
            325701,	//Siphon Life
            326607,	//Turn to Stone
            323552,	//Volley of Power
            323538,	//Bolt of Power
			//Mists of Tirna
            326021,	//Acid Globule
			331718,	//Spear Flurry
			331743,	//Bucking Rampage
		    322486,	//Overgrowth
            322938,	//Harvest Essence
            324914,	//Nourish the Forest
            324776,	//Bramblethorn Coat
            326046,	//Stimulate Resistance
            340544,	//Stimulate Regeneration
            337235,	//Parasitic Pacification
            337251,	//Parasitic Incapacitation
            337253,	//Parasitic Domination
            322450,	//Consumption
            322527,	//Gorging Shield
            321828,	//Patty Cake
			// Necrotic Wake
            335143,	//Bonemend
            334748,	//Drain Fluids
            320462,	//Necrotic Bolt
            324293,	//Rasping Scream
            320170,	//Necrotic Bolt
            338353,	//Goresplatter
            323190,	//Meat Shield
            327130,	//Repair Flesh
            322493,	//Frostbolt Volley
			// Plaguefall
            328400,	//Stealthlings
            318949,	//Festering Belch
            328177,	//Fungistorm
            330403,	//Wing Buffet
			336451,	//Bulwark of Maldraxxus
            328429,	//Crushing Embrace
            319070,	//Corrosive Gunk
            328180,	//Gripping Infection
            321999,	//Viral Globs
            328094,	//Pestilence Bolt
            328016,	//Wonder Grow
            328338,	//Call Venomfang
			// Sanguine Depths
			324609,	//Animate Weapon
            335305,	//Barbed Shackles
            319654,	//Hungering Drain
            322433,	//Stoneskin
            321038,	//Wrack Soul
            334653,	//Engorge
            335305,	//Barbed Shackles
            336277,	//Explosive Anger
            326952,	//Fiery Cantrip
            326836,	//Curse of Suppression
			// Spires of Ascension 
            327413, //  Rebellious Fist
            317936, // Forsworn Doctrine
            317963,	// Burden of Knowledge
            328295,	// Greater Mending
            328137,	// Dark Pulse
            328331,	// Forced Confession
			// Theater of Pain
            341902, //Unholy Fervor
            341969,	//Withering Discharge
            342139,	//Battle Trance
            330562,	//Demoralizing Shout
            330810,	//Bind Soul
            330868,	//Necrotic Bolt Volley
            341771,	//Grave Spike
            330532,	//Jagged Quarrel
            330875,	//Spirit Frost
            319669,	//Spectral Reach
            324589,	//Death Bolt
            342675,	//Bone Spear
            330586,	//Devour Flesh
            // Thorgast
            263085, // Terrifying Roar
            294526, // Curse of Frailty
            298844, // Fearsome Howl
            332165, // Fearsome Shriek
            329930, // Terrifying Screeh
            294517, // Phasing Roar
            296839, // Dearth Blast
            294165, // Accursed Strength
            330118, // Withering Roar
            258935, // Inner Flames
            277040, // Soul of Mist
            242391, // Terror
            330573, // Bounty of The Forest
            345554, // Stygian Shield
            327461, // Meat Hook
            330479, // Gunk
            310392, // intimidating Presence
            184381, // Interrupting Slam
            334538, // Deaden Magic
            330755, // Focused Blast
            295929, // Rats!
            332181, // Mass Devour
            297966, // Devour Obleron Armaments
            // Tasavezh
            355930, // Spark Burn
            355934, // Hard Light Barrier
            354297, // Hyper Light Bolt
            356537, // Empowered Glyph of Restraint
            347775, // Spam Filter
            355057, // Cry of Mrrggllrrgg
            355225, // Water Bolt
            347903, // Junk Mail
            358131, // Lightning Nova
            350922, // Menacing Shout
            357404, // Dischordant Song
            156877, // Double Technique
            347152, // Triple Technique
            351119, // Shuriken Blitz
            346980, // Empowered Defense
            352347, // Valorous Bolt
            355132, // Invigorating Fish Stick
            // Affixes Season 2
            355737, // Scorching Blast
            358967, // Inferno
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
                bool ShouldKickMO = Aimsharp.CustomFunction("ShouldKickMO") == 1;

                if (Class == "Monk" || Class == "Guardian Druid" || Class == "Death Knight" || Class == "Rogue" || Class == "Demon Hunter" || Class == "Warrior")
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
                                //always kick big cc spells and special spells
                                if (Raid.Contains(t.CastingID) && (!t.IsChanneling && t.CastingRemaining < KickValue || t.IsChanneling && t.CastingElapsed > KickChannelsAfter) && t.IsInterruptable && t.CastingElapsed > MinimumDelay)
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

                                //kick big damage spells if lowest hp ally is medium-low hp
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

                        if (ShouldKickMO && Aimsharp.CanCast(Interrupt, "target"))
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
                                //always kick big cc spells and special spells
                                if (Raid.Contains(t.CastingID) && (!t.IsChanneling && t.CastingRemaining < KickValue || t.IsChanneling && t.CastingElapsed > KickChannelsAfter) && t.CastingElapsed > MinimumDelay)
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

                                //kick big damage spells if lowest hp ally is medium-low hp
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
                        if (ShouldKickMO && Aimsharp.CanCast(CCInterrupt, "target"))
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
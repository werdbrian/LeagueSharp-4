using System;
using System.Collections.Generic;
using System.Linq;
using myOrbwalker;
using LeagueSharp.Common;
using LeagueSharp;

namespace Malzahar
{
    class Program
    {
        private const string ChampionName = "Malzahar";
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static bool IsChanneling;
        private static int VoidlingsCount;        
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 900f);
            W = new Spell(SpellSlot.W, 800f);
            E = new Spell(SpellSlot.E, 650f);
            R = new Spell(SpellSlot.R, 700f);

            Q.SetSkillshot(0.5f, 100, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.5f, 240, 20, false, SkillshotType.SkillshotCircle);

            Config = new Menu("Malzahar", "Malzahar", true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            MYOrbwalker.AddToMenu(orbwalkerMenu);
            Config.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Keys", "Keys"));
            Config.SubMenu("Keys").AddItem(new MenuItem("ComboActive", "Combo").SetValue(new KeyBind(Config.Item("Combo_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //x
            Config.SubMenu("Keys").AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind(Config.Item("Harass_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //s
            Config.SubMenu("Keys").AddItem(new MenuItem("LastHitActive", "Last Hit").SetValue(new KeyBind(Config.Item("LastHit_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //a
            Config.SubMenu("Keys").AddItem(new MenuItem("LaneClearActive", "Lane Clear").SetValue(new KeyBind(Config.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //c
            Config.SubMenu("Keys").AddItem(new MenuItem("FreezeActive", "Lane Freeze").SetValue(new KeyBind(Config.Item("LaneFreeze_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //z
            Config.SubMenu("Keys").AddItem(new MenuItem("JungleClearActive", "Jungle Clear").SetValue(new KeyBind(Config.Item("JungleClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press))); //v
            Config.SubMenu("Keys").AddItem(new MenuItem("FleeActive", "Flee").SetValue(new KeyBind(Config.Item("Flee_Key").GetValue<KeyBind>().Key, KeyBindType.Press)));  //space
            
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));   
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("CRType", "R").SetValue(new StringList(new[] { "Killable", "Voidlings" })));
            Config.SubMenu("Combo").AddItem(new MenuItem("VoidlingsE", "Voidlings with E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItemCombo", "Use Items").SetValue(true));
            
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmMana", "Farm Mana >").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Jungle Farm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("FountainVoildings", "Ready Voidlings").SetValue(true));

            Config.AddToMainMenu();
            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Game.OnUpdate += Game_OnUpdate;
        }
        private static void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe && spell.SData.Name.ToLower() == "alzaharnethergrasp")
            {
                IsChanneling = true;
                Utility.DelayAction.Add(2500, () => IsChanneling = false);
            }
        }
        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;
            MYOrbwalker.SetAttack(!Channeling());
            MYOrbwalker.SetMovement(!Channeling());
            if (Channeling()) return;
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active) LaneClear();     
            if (Config.Item("JungleClearActive").GetValue<KeyBind>().Active) JungleClear(); 
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();        
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active) Combo();
            if (Config.Item("FountainVoildings").GetValue<bool>()) AutoVoidlings();
        }
        private static void LaneClear()
        {
            if (GetPlayerManaPercentage() < Config.Item("FarmMana").GetValue<Slider>().Value) return;
            if (Config.Item("UseQFarm").GetValue<bool>() && Q.IsReady())
            {
                List<Obj_AI_Base> MinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                MinionManager.FarmLocation QLine = Q.GetCircularFarmLocation(MinionsQ);
                if (QLine.MinionsHit >= 3)
                {
                    Q.Cast(QLine.Position);
                }
            }
            if (Config.Item("UseWFarm").GetValue<bool>() && W.IsReady())
            {
                List<Obj_AI_Base> MinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                MinionManager.FarmLocation WCircular = W.GetCircularFarmLocation(MinionsW);
                if (WCircular.MinionsHit >= 4)
                {
                    W.Cast(WCircular.Position);
                }
            }
            if (Config.Item("UseEFarm").GetValue<bool>() && E.IsReady())
            {
                Obj_AI_Base tempTarget = null;
                float[] maxhealth;
                maxhealth = new float[] { 0 };
                var maxhealth2 = maxhealth;
                foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(
                    minion =>
                        minion.IsValidTarget(E.Range) && minion.Team != GameObjectTeam.Neutral).Where(
                        minion => minion.MaxHealth > maxhealth2[0] || Math.Abs(maxhealth2[0] - float.MaxValue) < float.Epsilon))
                {
                    tempTarget = minion;
                    maxhealth[0] = minion.MaxHealth;
                }
                if (tempTarget != null)
                {
                    if (ObjectManager.Player.GetSpellDamage(tempTarget, SpellSlot.E) < tempTarget.Health && (VoidlingIsReady() || VoidlingsTotal() >= 1))
                    {
                        E.Cast(tempTarget);
                    }
                }
            }
        }
        private static void JungleClear()
        {
            if (Config.Item("UseQJFarm").GetValue<bool>() && Q.IsReady())
            {
                List<Obj_AI_Base> MobsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                MinionManager.FarmLocation QLine = Q.GetCircularFarmLocation(MobsQ);
                if (QLine.MinionsHit > 1)
                {
                    Q.Cast(QLine.Position);
                }
            }
            if (Config.Item("UseWJFarm").GetValue<bool>() && W.IsReady())
            {
                List<Obj_AI_Base> MobsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                MinionManager.FarmLocation WCircular = W.GetCircularFarmLocation(MobsW);
                if (WCircular.MinionsHit > 1)
                {
                    W.Cast(WCircular.Position);
                }
            }
            if (Config.Item("UseEJFarm").GetValue<bool>() && E.IsReady())
            {
                List<Obj_AI_Base> MobsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.Health);
                if (VoidlingIsReady() || VoidlingsTotal() >= 1)
                {
                    E.Cast(MobsE[0]);
                }
            }
        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical, false);
            var UseQ = Config.Item("UseQCombo").GetValue<bool>();
            var UseW = Config.Item("UseWCombo").GetValue<bool>();
            var UseE = Config.Item("UseECombo").GetValue<bool>();
            var UseR = Config.Item("UseRCombo").GetValue<bool>();
            var UseVoidlingsE = Config.Item("VoidlingsE").GetValue<bool>();
            var RType = Config.Item("CRType").GetValue<StringList>().SelectedIndex;
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() && Q.IsInRange(target))
                {
                    Q.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                }
                if (UseW && W.IsReady())
                {
                    W.Cast(target.ServerPosition);
                }
                if (UseE && E.IsReady())
                {
                    if (UseVoidlingsE)
                    {
                        if (VoidlingIsReady() || VoidlingsTotal() >= 1)
                        {
                            E.CastOnUnit(target);
                        }
                    }
                    else E.CastOnUnit(target);
                }
                if (UseR && R.IsReady() && !Q.IsReady() && !W.IsReady() && !E.IsReady())
                {
                    switch (RType)
                    {
                        case 0:
                            if (R.IsKillable(target)) R.CastOnUnit(target);
                            break;
                        case 1:
                            if ((VoidlingIsReady() || VoidlingsTotal() >= 1)) R.CastOnUnit(target);
                            break;
                    }
                }
            }
        }
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var UseQ = Config.Item("UseQHarass").GetValue<bool>();
            var UseW = Config.Item("UseWHarass").GetValue<bool>();
            var UseE = Config.Item("UseEHarass").GetValue<bool>();
            if (target.IsValidTarget())
            {
                if (UseQ && Q.IsReady() && Q.IsInRange(target))
                {
                    Q.Cast(target.Position);
                }
                if (UseW && W.IsReady() && W.IsInRange(target))
                {
                    WPredict(target);
                }
                if (UseE && E.IsReady() && E.IsInRange(target))
                {
                    if (VoidlingIsReady() || VoidlingsTotal() >= 1)
                    {
                        E.CastOnUnit(target);
                    }
                }
            }
        }
        private static void AutoVoidlings()
        {
            if (!VoidlingIsReady() && !ObjectManager.Player.InShop() && ObjectManager.Player.InFountain())
            {
                switch (VoidlingSummoningCount())
                {
                    case 0:
                        if (Q.IsReady()) Q.Cast(ObjectManager.Player.ServerPosition);
                        if (W.IsReady()) W.Cast(ObjectManager.Player.ServerPosition);
                        break;
                    case 1:
                        if (Q.IsReady()) Q.Cast(ObjectManager.Player.ServerPosition);
                        if (W.IsReady()) W.Cast(ObjectManager.Player.ServerPosition);
                        break;
                    case 2:
                        if (Q.IsReady()) Q.Cast(ObjectManager.Player.ServerPosition);
                        break;
                }
            }
        }
        private static float GetComboDamage(Obj_AI_Base target)
        {
            var damage = 0d;
            return (float)damage;
        }
        private static bool Killable(Obj_AI_Base target)
        {
            return target.Health < GetComboDamage(target);
        }
        private static void UseItems(int type, Obj_AI_Base target)
        {
            Int16[] SelfItems = { 3142 }; //3180, 3131, 3074, 3077, 
            Int16[] TargetingItems = { 3042 }; //3153, 3144, 3188, 3128, 3146, 3184 
            Int16[] AoeItems = { 3074, 3077 }; //3180, 3131, 3074, 3077, 
            switch (type)
            {
                case 0:
                    foreach (var itemId in SelfItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
                    {
                        Items.UseItem(itemId);
                    }
                    break;
                case 1:
                    foreach (var itemId in TargetingItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
                    {
                        if (target != null) Items.UseItem(itemId, target);
                    }
                    break;
                case 2:
                    foreach (var itemId in AoeItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
                    {
                        Items.UseItem(itemId);
                    }
                    break;
            } 
        }
        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }
        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }
        private static bool Channeling()
        {
            if (IsChanneling) { return true; }
            return ObjectManager.Player.HasBuff("alzaharnethergraspsound");
        }
        private static int VoidlingSummoningCount()
        {
            foreach (var buffs in ObjectManager.Player.Buffs.Where(buffs => buffs.Name == "alzaharvoidlingcount"))
            {
                return buffs.Count;
            }
            return 0;
        }
        private static bool VoidlingIsReady()
        {
            return ObjectManager.Player.HasBuff("alzaharsummonvoidling");
        }
        private static int VoidlingsTotal()
        {
             var voidlings = ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValid && minion.IsAlly && minion.BaseSkinName.Contains("voidling"));
             if (voidlings != null)
             {
                 return voidlings.Count();
             }
             return 0;
        }
        private static void WPredict(Obj_AI_Base target)
        {            
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(W.Range) && target != champ select champ).ToList();
            if (nearChamps.Count > 0)
            {
                var closeToPrediction = new List<Obj_AI_Hero>();
                foreach (var enemy in nearChamps)
                {
                    PredictionOutput prediction = W.GetPrediction(enemy);
                    if (prediction.Hitchance == HitChance.High && (ObjectManager.Player.Distance(enemy) < W.Range))
                    {
                        closeToPrediction.Add(enemy);
                    }
                }
                if (closeToPrediction.Count > 0)
                {
                    W.CastIfWillHit(target, closeToPrediction.Count, false);

                }
            }
        }
    }
}
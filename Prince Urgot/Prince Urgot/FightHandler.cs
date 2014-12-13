﻿#region

using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace Prince_Urgot
{
    internal class FightHandler
    {
        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (MenuHandler._uMenu.Item("autoInt").GetValue<bool>() && SkillHandler.R.IsReady() && unit.IsEnemy &&
                unit.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
            {
                SkillHandler.R.CastOnUnit(unit, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
        }

        public static void Hunter()
        {
            var target = SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical);
            if (SkillHandler.Q.IsReady() &&
                target.IsValidTarget(
                    target.HasBuff("urgotcorrosivedebuff", true) ? SkillHandler.Q2.Range : SkillHandler.Q.Range))
            {
                SkillHandler.Q.Cast(target, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
        }

        public static void SmartQ()
        {
            foreach (var obj in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(obj => obj.IsValidTarget(SkillHandler.Q2.Range) && obj.HasBuff("urgotcorrosivedebuff", true))
                )
            {
                SkillHandler.Q2.Cast(obj.ServerPosition, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
        }

        public static void CastLogic()
        {
            var CastQ = (MenuHandler._uMenu.Item("ComboQ").GetValue<bool>());
            var CastE = (MenuHandler._uMenu.Item("ComboE").GetValue<bool>());
            var CastW = (MenuHandler._uMenu.Item("ComboW").GetValue<bool>());

            SmartQ();


            var target = SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (CastE)
            {
                Ncc();
            }
            if (CastQ)
            {
                Hunter();
            }
            if (CastW)
            {
                Shield();
            }
        }

        public static void Shield()
        {
            var target = SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical);
            var distance = ObjectManager.Player.Distance(target);

            if (SkillHandler.W.IsReady() && distance <= 100 || (distance >= 900 && distance <= 1200))
            {
                SkillHandler.W.Cast(MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
        }

        public static void Ncc()
        {
            if (!SkillHandler.E.IsReady())
            {
                return;
            }

            var hitchance = (HitChance) (MenuHandler._uMenu.Item("preE").GetValue<StringList>().SelectedIndex + 3);
            var target = SimpleTs.GetTarget(1400, SimpleTs.DamageType.Physical);

            if (target.IsValidTarget(SkillHandler.E.Range))
            {
                SkillHandler.E.CastIfHitchanceEquals(
                    target, hitchance, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
            else
            {
                SkillHandler.E.CastIfHitchanceEquals(
                    SimpleTs.GetTarget(SkillHandler.E.Range, SimpleTs.DamageType.Physical), HitChance.High,
                    MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
        }

        public static void Harass()
        {
            var mana = Player.Mana >
                       Player.MaxMana * MenuHandler._uMenu.Item("HaraManaPercent").GetValue<Slider>().Value / 100;
            var target = SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical);

            if (MenuHandler._uMenu.Item("haraQ").GetValue<bool>())
            {
                SmartQ();
                Shield();
            }

            if (MenuHandler._uMenu.Item("haraE").GetValue<bool>() && mana)
            {
                if (target == null)
                {
                    return;
                }

                Ncc();
            }
        }

        public static void KillSteal()
        {
            var target = SimpleTs.GetTarget(SkillHandler.Q.Range, SimpleTs.DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (target.IsValidTarget(SkillHandler.Q.Range) && SkillHandler.Q.IsReady() &&
                MenuHandler._uMenu.Item("KillQ").GetValue<bool>() &&
                ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) > target.Health)
            {
                Hunter();
            }

            if (MenuHandler._uMenu.Item("KillI").GetValue<bool>())
            {
                if (ItemHandler.IgniteSlot != SpellSlot.Unknown &&
                    Player.Spellbook.CanUseSpell(ItemHandler.IgniteSlot) == SpellState.Ready)
                {
                    if (target.Health + 20 <= ItemHandler.GetIgniteDamage(target))
                    {
                        Player.Spellbook.CastSpell(ItemHandler.IgniteSlot, target);
                    }
                }
            }
        }

        public static void LaneClear()
        {
            if (MenuHandler._uMenu.Item("LaneClearQ").GetValue<bool>())
            {
                if (Player.Mana >
                    Player.MaxMana * MenuHandler._uMenu.Item("LaneClearQManaPercent").GetValue<Slider>().Value / 100)
                {
                    var myMinions = MinionManager.GetMinions(
                        Player.ServerPosition, Player.AttackRange, MinionTypes.All, MinionTeam.NotAlly);
                    var castQ = MenuHandler._uMenu.Item("LaneClearQ").GetValue<bool>();
                    var castE = MenuHandler._uMenu.Item("LaneClearE").GetValue<bool>();

                    if (castE && SkillHandler.E.IsReady())
                    {
                        foreach (var minion in myMinions.Where(minion => minion.IsValidTarget()))
                        {
                            if (minion.IsValidTarget(SkillHandler.E.Range))
                            {
                                SkillHandler.E.Cast(minion, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
                            }
                        }
                    }

                    if (castQ && SkillHandler.Q.IsReady())
                    {
                        foreach (var minion in myMinions.Where(minion => minion.IsValidTarget()))
                        {
                            if (Vector3.Distance(minion.ServerPosition, Player.ServerPosition) <= SkillHandler.Q2.Range &&
                                minion.HasBuff("urgotcorrosivedebuff", true))
                            {
                                SkillHandler.Q2.Cast(
                                    minion.ServerPosition, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
                            }
                            if (Vector3.Distance(minion.ServerPosition, Player.ServerPosition) <= SkillHandler.Q.Range)
                            {
                                SkillHandler.Q.Cast(
                                    minion.ServerPosition, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
                            }
                        }
                    }
                }
            }
        }

        public static void LastHit()
        {
            if (MenuHandler._uMenu.Item("lastHitQ").GetValue<bool>())
            {
                if (Player.Mana >
                    Player.MaxMana * MenuHandler._uMenu.Item("LaneClearQManaPercent").GetValue<Slider>().Value / 100)
                {
                    var myMinions = MinionManager.GetMinions(Player.ServerPosition, SkillHandler.Q.Range);

                    if (SkillHandler.Q.IsReady())
                    {
                        foreach (var minion in
                            myMinions.Where(
                                minion =>
                                    Player.GetSpellDamage(minion, SpellSlot.Q) >=
                                    HealthPrediction.GetHealthPrediction(minion, (int) (SkillHandler.Q.Delay * 1000))))
                        {
                            SkillHandler.Q.Cast(
                                minion.ServerPosition, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
                        }
                    }
                }
            }
        }

        public static void ActivateMura()
        {
            if (MenuHandler._uMenu.Item("useMura").GetValue<KeyBind>().Active)
            {
                if (Player.Buffs.Count(buf => buf.Name == "Muramana") == 0)
                {
                    ItemHandler.Muramana.Cast();
                }
            }
        }

        public static void DeActivateMura()
        {
            if (MenuHandler._uMenu.Item("useMura").GetValue<KeyBind>().Active)
            {
                if (Player.Buffs.Count(buf => buf.Name == "Muramana") == 1)
                {
                    ItemHandler.Muramana.Cast();
                }
            }
        }

        public static void AutoR()
        {
            var target = SimpleTs.GetTarget(SkillHandler.R.Range, SimpleTs.DamageType.Physical);

            var turret = ObjectManager.Get<Obj_AI_Turret>().First(obj => obj.IsAlly && obj.Distance(Player) <= 775f);

            if (turret != null && target != null)
            {
                Ncc();
                SkillHandler.R.Cast(target, true, MenuHandler._uMenu.Item("Packet").GetValue<bool>());
            }
        }
    }
}
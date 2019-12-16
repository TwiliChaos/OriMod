using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;

namespace OriMod.Abilities {
  public class WallChargeJump : Ability {
    public WallChargeJump(OriAbilities handler) : base(handler) { }
    public override int id => AbilityID.WallChargeJump;

    internal override bool DoUpdate => InUse || oPlayer.Input(OriMod.ChargeKey.Current);
    internal override bool CanUse => base.CanUse && Charged && CanCharge;
    protected override int Cooldown => (int)(Config.WCJumpCooldown * 30);
    protected override Color RefreshColor => Color.Blue;

    internal bool CanCharge => base.CanUse && Handler.climb.IsCharging;
    private int MaxCharge => 35;
    private int Duration => 20;
    private static readonly float[] Speeds = new float[20] {
      100f, 99.5f, 99, 98.5f, 97.5f, 96.3f, 94.7f, 92.6f, 89.9f, 86.6f, 82.8f, 76f, 69f, 61f, 51f, 40f, 30f, 22f, 15f, 12f
    };
    private float SpeedMultiplier => Config.WCJumpSpeedMultipler * 0.5f;
    private float MaxAngle => Config.WCJumpMaxAngle;

    internal bool Charged;
    private int CurrCharge;
    private Vector2 Direction;

    public Projectile Proj { get; private set; }

    internal Vector2 GetMouseDirection() => GetMouseDirection(out float _);
    internal Vector2 GetMouseDirection(out float angle) {
      Vector2 mouse = Main.MouseWorld - player.Center;
      mouse.X *= -Handler.climb.WallDir;
      mouse.Y *= player.gravDir;
      mouse += player.Center;
      angle = Utils.Clamp(player.AngleTo(mouse), -MaxAngle, MaxAngle);
      Vector2 dir = Vector2.UnitX.RotatedBy(angle);
      dir.X *= -Handler.climb.WallDir;
      dir.Y *= player.gravDir;
      return dir;
    }

    private void StartWallChargeJump() {
      oPlayer.PlayNewSound("Ori/ChargeJump/seinChargeJumpJump" + OriPlayer.RandomChar(3, ref CurrSoundRand));
      Charged = false;
      CurrCharge = 0;
      Proj = Main.projectile[Projectile.NewProjectile(player.Center, Vector2.Zero, oPlayer.mod.ProjectileType("ChargeJumpProjectile"), 30, 0f, player.whoAmI, 0, 1)];
      PutOnCooldown();
      Direction = GetMouseDirection();
      player.velocity = Direction * Speeds[0] * SpeedMultiplier;
    }

    private void UpdateCharged() {
      if (Main.rand.NextFloat() < 0.7f) {
        Dust.NewDust(player.Center, 12, 12, oPlayer.mod.DustType("AbilityRefreshedDust"), newColor: Color.Blue);
      }
    }

    protected override void UpdateActive() {
      float speed = Speeds[CurrTime - 1] * SpeedMultiplier;
      player.velocity = Direction * speed;
      player.direction = Math.Sign(player.velocity.X);
      player.maxFallSpeed = Math.Abs(player.velocity.Y);
    }

    protected override void UpdateUsing() {
      player.controlJump = false;
    }

    internal override void Tick() {
      if (Handler.burrow.InUse) {
        Charged = false;
        CurrCharge = 0;
        return;
      }
      TickCooldown();
      if (!Charged && CanCharge) {
        if (CurrCharge == 0) {
          oPlayer.PlayNewSound("Ori/ChargeJump/seinChargeJumpChargeB", 1f, .2f);
        }
        CurrCharge++;
        if (CurrCharge > MaxCharge) {
          Charged = true;
          oPlayer.PlayNewSound("Ori/ChargeJump/seinChargeJumpChargeB", 1f, .2f);
        }
      }
      if (CanUse && oPlayer.Input(PlayerInput.Triggers.JustPressed.Jump)) {
        StartWallChargeJump();
        Active = true;
      }
      else if (Charged) {
        UpdateCharged();
        if (!CanCharge) {
          Charged = false;
          CurrCharge = 0;
          oPlayer.PlayNewSound("Ori/ChargeDash/seinChargeDashUncharge", 1f, .3f);
        }
      }
      if (Active) {
        CurrTime++;
        if (CurrTime > Duration) {
          if (oPlayer.Input(PlayerInput.Triggers.Current.Jump)) {
            Ending = true;
          }
          else {
            Inactive = true;
          }
          CurrTime = 0;
        }
      }
      if (Ending && !oPlayer.Input(PlayerInput.Triggers.Current.Jump)) {
        Inactive = true;
      }
    }
  }
}

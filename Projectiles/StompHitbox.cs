using Terraria;
using Terraria.ModLoader;

namespace OriMod.Projectiles {
  public class StompHitbox : ModProjectile {
    private OriPlayer _owner;
    public OriPlayer Owner => _owner ?? (_owner = Main.player[projectile.owner].GetModPlayer<OriPlayer>());
    private Abilities.Stomp stomp => Owner.stomp;
    public override void SetStaticDefaults() { }

    public override void SetDefaults() {
      projectile.width = 40;
      projectile.height = 56;
      projectile.timeLeft = 2;
      projectile.penetrate = 999;
      projectile.magic = true;
      projectile.tileCollide = false;
      projectile.ignoreWater = true;
      projectile.friendly = true;
    }
    public override void AI() {
      projectile.Center = Main.player[projectile.owner].Center;
      if (stomp.Active) {
        projectile.position.Y += 10;
        projectile.timeLeft = 2;
      }
    }
    public override bool ShouldUpdatePosition() => false;

    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
      if (Main.rand.Next(5) == 1) {
        crit = true;
      }
      if (target.life > 0 && Owner.stomp.InUse) {
        Main.NewText("This would make Stomp stop.");
        Owner.stomp.EndStomp();
      }
    }
  }
}
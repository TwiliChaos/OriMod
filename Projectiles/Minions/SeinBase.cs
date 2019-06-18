using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameInput;
using Microsoft.Xna.Framework.Graphics;

namespace OriMod.Projectiles.Minions {
  public abstract class SeinBase : Minion {
    protected int Upgrade;
    public override string Texture => "OriMod/Projectiles/Minions/Sein";
    public override bool? CanCutTiles() => false;
    protected bool Autoshoot {
      get {
        Player player = Main.player[projectile.owner];
        Item heldItem = player.HeldItem;
        return !(heldItem.IsAir || heldItem.shoot == projectile.type);
      }
    }
    public override void SetStaticDefaults() {
      Main.projFrames[projectile.type] = 3;
      Main.projPet[projectile.type] = true;
      ProjectileID.Sets.MinionSacrificable[projectile.type] = true;
      ProjectileID.Sets.Homing[projectile.type] = true;
      ProjectileID.Sets.MinionTargettingFeature[projectile.type] = true; //This is necessary for right-click targeting
    }
    public override void SetDefaults() {
      projectile.netImportant = true;
      projectile.friendly = true;
      projectile.minion = true;
      projectile.minionSlots = -0.001f;
      projectile.penetrate = -1;
      projectile.timeLeft = 18000;
      projectile.tileCollide = false;
      projectile.ignoreWater = true;
      projectile.velocity = new Vector2(0, -maxVelocityInBounds);
      projectile.position = PlayerSpace();
      targetSpawn = PlayerSpace(0, -32);
      minionTargetLocation = PlayerSpace(0, -32);
    }
    protected int Cooldown {
      get {
        return (int)projectile.ai[0];
      }
      set {
        if (value != projectile.ai[0]) {
          projectile.netUpdate = true;
        }
        projectile.ai[0] = value;
      }
    }

    // ID of projectile to shoot
    protected int ShootID;
    // Number of shots that can be used before triggering longCooldown
    protected int MaxShotsPerBurst = 2;
    // Max number of shots that can be fired at once
    protected int MaxShotsPerVolley = 1;
    protected int ShotsToTarget = 1;
    protected int ShotsToPrimaryTarget = 1;
    private int currShots = 1;
    // Maximum number of targets that can be fired upon at once
    protected int MaxTargets = 3;
    // Shortest cooldown between individual shots
    protected float MinCooldown = 12f;
    // Shortest cooldown to count as a seperate burst and not be punished by longCooldown
    protected float ShortCooldown = 18f;
    // Cooldown between bursts of shots dictated by numShots
    protected float LongCooldown = 60f;
    // Speed of the created projectile
    protected float ShootSpeed = 50f;
    protected int Pierce = 1;
    protected float PrimaryDamageMultiplier = 1;
    // Max rotation of randomness from target the projectile fires
    protected float ManualShootDamageMultiplier = 1.4f;
    protected int RandDegrees = 75;
    protected float MaxTargetDist = 300f;
    protected float MaxTargetThroughWallDist = 0f;

    protected Color Color;
    protected float LightStrength;
    internal void Init(int upgradeID) {
      SeinUpgrade u = OriMod.SeinUpgrades[upgradeID - 1];
      MaxShotsPerBurst = u.shotsPerBurst;
      MaxShotsPerVolley = u.maxShotsPerVolley;
      ShotsToTarget = u.shotsPerTarget;
      ShotsToPrimaryTarget = u.shotsToPrimaryTarget;
      PrimaryDamageMultiplier = u.primaryDamageMultiplier;
      Pierce = u.pierce;
      MaxTargets = u.targets;
      MinCooldown = u.minCooldown;
      ShortCooldown = u.shortCooldown;
      LongCooldown = u.longCooldown;
      RandDegrees = u.randDegrees;
      MaxTargetDist = u.targetMaxDist;
      if (maxDistFromPlayer < MaxTargetDist * 0.8f) { 
        maxDistFromPlayer = MaxTargetDist * 0.8f;
      }
      MaxTargetThroughWallDist = u.targetThroughWallDist;
      projectile.width = u.minionWidth;
      projectile.height = u.minionHeight;

      Upgrade = upgradeID;
      ShootID = mod.ProjectileType("SpiritFlame" + (upgradeID));
      Color = u.color;
      LightStrength = u.lightStrength;

      
      SpiritFlameSound =
        Upgrade == 1 || Upgrade == 2 ? "" :
        Upgrade == 3 || Upgrade == 4 ? "LevelB" :
        Upgrade == 5 || Upgrade == 6 ? "LevelC" :
        Upgrade == 7 || Upgrade == 8 ? "LevelD" : "";
    }
    public void SetUpgrade(int upgrade) {
      Init(upgrade);
    }
    protected virtual void CreateDust() { }
    protected virtual void SelectFrame() { }
    
    private float Lerp(float firstFloat, float secondFloat, float by) {
     return firstFloat * (1 - by) + secondFloat * by;
    }
    private Vector2 minionTargetLocation;
    private Vector2 targetSpawn;
    private NPC mainTargetNPC;
    private int targetSide = 1;
    private Vector2 PlayerSpace(float x, float y) {
      return PlayerSpace(new Vector2(x, y));
    }
    private Vector2 PlayerSpace(Vector2 coords=new Vector2()) {
      return Main.player[projectile.owner].position + coords;
    }
    
    // You will definitely need to tweak these.
    protected float minVelocity = 0.1f;        // This is the slowest speed Sein can be at.
    protected float maxVelocityInBounds = 1.32f;        // This is the fastest speed Sein can be at.
    protected float maxVelocityOutOfBounds = 30f;
    protected float nearThreshold = 13f;    // This is the distance from which Sein will begin to slow down.
    protected float damping = 0.9f;    // This is how much Sein's speed is reduced every time Behavior() is called should she be closer than SeinNearThreshold.
    protected float maxDampingOutOfBounds = 0.75f;
    protected float accelerationInBounds = 1.06f;
    protected float accelerationOutofBounds = 1.1f;
    protected float triggerTargetMove = 0.5f;
    protected float maxDistFromPlayer = 240f;
    protected float minDistFromNPC = 64f;
    protected string SpiritFlameSound;
    protected SoundEffectInstance PlaySpiritFlameSound(string Path, float Volume) {
      return Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, ("Sounds/Custom/NewSFX/Ori/SpiritFlame/" + Path)).WithVolume(Volume), projectile.Center);
    }
    private int excludeRand = 0;
    private static readonly Vector2[] TargetPositions = new Vector2[] {
      new Vector2(-24, 7),
      new Vector2(24, -7),
      new Vector2(-24, -2),
      new Vector2(24, 7),
      new Vector2(-24, -7),
      new Vector2(24, -2),
    };
    private static readonly Vector2 bounds = new Vector2(68f, 32f);
    private int targetPosIndex = 0;
    private List<byte> targetIDs = new List<byte>();
    private static Vector2 Normalize(Vector2 vec2) {
        return vec2 / vec2.Length();
    }
    
    private void SetNewMinionTarget(int idx=-1) {
      targetPosIndex = idx != -1 ? idx : targetPosIndex + 1;
      if (targetPosIndex >= TargetPositions.Length) {
        targetPosIndex = 0;
      }
      minionTargetLocation = targetSpawn + TargetPositions[targetPosIndex];
      targetSide = -targetSide;
    }
    private bool IsInBounds() {
      Vector2 p = projectile.position;
      return (
        p.X < targetSpawn.X + bounds.X && 
        p.X > targetSpawn.X - bounds.X && 
        p.Y < targetSpawn.Y + bounds.Y && 
        p.Y > targetSpawn.Y - bounds.Y
      );
    }
    private int SortByDistClosest(byte id1, byte id2) {
      Vector2 playerPos =  Main.player[projectile.owner].position;
      float length1 = (Main.npc[id1].position - playerPos).Length();
      float length2 = (Main.npc[id2].position - playerPos).Length();
      return length1.CompareTo(length2);
    }
    internal override void CheckActive() {
      Player player = Main.player[projectile.owner];
      OriPlayer oPlayer = player.GetModPlayer<OriPlayer>(mod);
      if (player.dead) {
        oPlayer.SeinMinionActive = false;
      }

      if (oPlayer.SeinMinionActive && Upgrade == oPlayer.SeinMinionUpgrade) {
        projectile.timeLeft = 2;
      }
    }
    // This is the somewhat subtle swaying about Sein does at any given time in Blind Forest
    private void SeinMovement() {
      Player owner = Main.player[projectile.owner];
      if (projectile.position.HasNaNs()) {
        projectile.position = owner.position;
      }
      if (projectile.velocity.HasNaNs()) {
        projectile.velocity = new Vector2(0, -maxVelocityInBounds);
      }
      Vector2 oldVel = projectile.velocity;
      if (oldVel.Length() == 0) { // Usually spawn, magnitide should never be 0 otherwise, and being 0 would break spawning
        oldVel = new Vector2(0, -maxVelocityInBounds);
      }
      Vector2 oldPos = projectile.position;
      Vector2 oldDir = Normalize(oldVel);
      if ((minionTargetLocation - PlayerSpace()).Length() > 1000 || (targetSpawn - PlayerSpace()).Length() > 1000) {
        minionTargetLocation = PlayerSpace(0, -32);
      }
      Vector2 newDir = Normalize(minionTargetLocation - oldPos);
      
      float dist = (minionTargetLocation - oldPos).Length();
      if (dist > 1050) { // Want to be 800
        projectile.position = PlayerSpace(-newDir * 1000f); // also 800
        projectile.velocity = newDir * maxVelocityOutOfBounds;
        return;
      }
      
      Vector2 newVel = oldVel.Length() * newDir;
      if (IsInBounds()) {
        if (dist < nearThreshold) {
          newVel *= damping;
        }
        else {
          newVel *= accelerationInBounds;
          if (newVel.Length() > maxVelocityInBounds) {
            newVel = newDir * Lerp(newVel.Length(), maxVelocityInBounds, 0.22f);
          }
        }
      }
      else {
        SetNewMinionTarget(newVel.X > 0 ? 3 : 1);
        newVel *= accelerationOutofBounds;
        if (newVel.Length() > maxVelocityOutOfBounds) { // Too fast... maybe
          if (newVel.Length() > owner.velocity.Length()) {
            newVel = newDir * Lerp(newVel.Length(), owner.velocity.Length(), 0.7f);
          }
          else {
            newVel = newDir * Lerp(newVel.Length(), maxVelocityOutOfBounds, 0.2f);
          }
        }
        if (newVel.Length() < (oldVel.Length() * maxDampingOutOfBounds)) { // Damned more than necessary
          newVel = newDir * Lerp(oldVel.Length(), newVel.Length(), maxDampingOutOfBounds);
        }
      }
      if (newVel.Length() < minVelocity * 2f) { // Too slow
        newVel = newDir * minVelocity * 2.1f;
        SetNewMinionTarget();
      }

      if (dist < nearThreshold || dist > 85) {
        projectile.velocity = Normalize(oldVel * 0.25f + newVel * 0.75f) * newVel.Length();
      }
      else {
        projectile.velocity = Normalize(oldVel * 0.8f + newVel * 0.2f) * newVel.Length();
      }
    }
    private void UpdateTargetPosIdle() {
      targetSpawn = PlayerSpace(0, -24f);
      minionTargetLocation = targetSpawn + TargetPositions[targetPosIndex];
    }
    private void UpdateTargetPosToNPC() {
      int mainTarget = targetIDs[0];
      Vector2 playerPos = Main.player[projectile.owner].position;
      Vector2 npcPos = Main.npc[mainTarget].position;
      Vector2 offset = PlayerSpace(0, -24f) - npcPos; 
      offset.Y -= 12f;
      Vector2 dir = Normalize(offset);
      float dist = offset.Length();

      if (dist > MaxTargetDist) { // Cannot reach targeted NPC
        if (targetIDs.Count == 1 || Main.player[projectile.owner].HasMinionAttackTargetNPC) {
          UpdateTargetPosIdle();
          return;
        }
        else { // Try reaching closest NPC
          mainTarget = targetIDs[1];
          npcPos = Main.npc[mainTarget].position;
          offset = PlayerSpace(0, -24f) - npcPos;
          offset.Y -= 12f;
          dist = offset.Length();
          dir = Normalize(offset);
          if (dist > MaxTargetDist) {
            UpdateTargetPosIdle();
            return;
          }
        }
      }
      if (dist < minDistFromNPC) {
        targetSpawn = PlayerSpace(0, -24f);
      }
      else if (dist > maxDistFromPlayer) {
        targetSpawn = PlayerSpace() - dir * maxDistFromPlayer;
      }
      else {
        targetSpawn = npcPos + dir * minDistFromNPC;
      }
      minionTargetLocation = targetSpawn + TargetPositions[targetPosIndex];
    }
    private void UpdateTargetsPos() {
      Vector2 projToTarget = (projectile.position - minionTargetLocation);
      if (projToTarget.Length() < triggerTargetMove && projectile.velocity.Length() > maxVelocityInBounds) {
        SetNewMinionTarget();
      }
      if (targetIDs.Count == 0 || Main.npc[targetIDs[0]].active == false) { // Idle movement above Ori
        UpdateTargetPosIdle();
      }
      else {
        UpdateTargetPosToNPC();
      }
    }
    private void Fire(int t) {
      Vector2 shootVel = Vector2.Zero;
      Vector2 nonTargetPos = projectile.position;
      float rand;
      if (t != -1) {
        shootVel = Main.npc[targetIDs[t]].position - projectile.Center;
        rand = (float)Main.rand.Next(-RandDegrees, RandDegrees) / 180f * (float)Math.PI;
      }
      else {
        shootVel = new Vector2(Main.rand.Next(-12, 12), Main.rand.Next(24, 48));
        rand = (float)Main.rand.Next(-180, 180) / 180f * (float)Math.PI;
        nonTargetPos.Y += Main.rand.Next(8, 48);
        nonTargetPos = Utils.RotatedBy(nonTargetPos, (float)Main.rand.NextFloat((float)Math.PI * 2));
      }
      if (shootVel == Vector2.Zero) {
        shootVel.Y = 1f;
      }
      shootVel.Normalize();
      shootVel = Utils.RotatedBy(shootVel, rand);
      shootVel *= ShootSpeed;
      int dmg = projectile.damage;
      dmg = (int)(dmg * Main.player[projectile.owner].minionDamage);
      if (t == 0) dmg = (int)(dmg * PrimaryDamageMultiplier);
      if (!Autoshoot) dmg = (int)(dmg * ManualShootDamageMultiplier);
      int proj = Projectile.NewProjectile(projectile.Center, shootVel, ShootID, dmg, projectile.knockBack, Main.myPlayer, 0, 0);
      projectile.velocity += (shootVel * -0.015f);
      if (t == -1) {
        Main.projectile[proj].ai[0] = nonTargetPos.X;
        Main.projectile[proj].ai[1] = nonTargetPos.Y;
      }
      else {
        Main.projectile[proj].ai[0] = targetIDs[t];
        Main.projectile[proj].ai[1] = 0;
      }
      Main.projectile[proj].timeLeft = t != -1 ? 300 : 15;
      Main.projectile[proj].netUpdate = true;
      Main.projectile[proj].penetrate = Pierce;
    }
    internal override void Behavior() {
      if (!projectile.active) { return; }
      SeinMovement();
      UpdateTargetsPos();
      Lighting.AddLight(projectile.Center, Color.ToVector3() * LightStrength);
      
      Player player = Main.player[projectile.owner];

      List<Vector2> targetPositions = new List<Vector2>();
      List<byte> newTargetIDs = new List<byte>();
      List<byte> wormIDs = new List<byte>();

      Vector2 targetPos = projectile.position;
      bool targeting = false;
      projectile.tileCollide = false;
      
      // If player specifies target, add that target to selection
      if(player.HasMinionAttackTargetNPC) {
        NPC npc = Main.npc[player.MinionAttackTargetNPC];
        if (npc.CanBeChasedBy(this, false)) {
          float distance = Vector2.Distance(player.Center, npc.Center);
          if (
            distance < MaxTargetDist && 
            (
              Collision.CanHitLine(projectile.position, projectile.width, projectile.height, npc.position, npc.width, npc.height) || 
              distance < MaxTargetThroughWallDist
            )
          ) {
            targeting = true;
            mainTargetNPC = npc;
          }
        }
      }

      // Otherwise set target based on different enemies, if they can hit
      for (int k = 0; k < Main.maxNPCs; k++) {
        NPC npc = Main.npc[k];
        if (!npc.CanBeChasedBy(this, false) || !npc.active) continue;
        float distance = Vector2.Distance(player.Center, npc.Center);
        if (
          distance < MaxTargetThroughWallDist || distance < MaxTargetDist &&
          Collision.CanHitLine(projectile.position, projectile.width, projectile.height, npc.position, npc.width, npc.height)
        ) {
          if (npc.aiStyle == 6 || npc.aiStyle == 37) { // TODO: Sort targeted worm piece by closest rather than whoAmI
            if (!wormIDs.Contains((byte)npc.ai[3])) {
              wormIDs.Add((byte)npc.ai[3]);
            }
            else {
              continue;
            }
          }
          targeting = true;
          newTargetIDs.Add((byte)npc.whoAmI);
        }
      }
      bool doReplace = false;
      int numExcepts = targetIDs.Except(newTargetIDs).Count();
      if (newTargetIDs.Count != targetIDs.Count || numExcepts != 0) { // See if list needs to be replaced
        doReplace = true; // Number of NPCs or the specific NPCs targeted was changed
      }
      else {
        float dist = 0;
        for (int t = 0; t < targetIDs.Count; t++) {
          float npcDist = (player.position - Main.npc[targetIDs[t]].position).Length();
          if (npcDist < dist) {
            doReplace = true; // List of NPCs is no longer in order of distance
            break;
          }
          else {
            dist = npcDist;
          }
        }
      }

      if (doReplace) { // Replace list
        if (newTargetIDs.Count > 1) {
          newTargetIDs.Sort(SortByDistClosest);
        }
        targetIDs.Clear();
        if (mainTargetNPC != null && mainTargetNPC.active) {
          targetIDs.Add((byte)mainTargetNPC.whoAmI);
          targetIDs.AddRange(newTargetIDs.GetRange(0, newTargetIDs.Count > MaxTargets - 1 ? MaxTargets - 1 : newTargetIDs.Count));
        }
        targetIDs.AddRange(newTargetIDs.GetRange(0, newTargetIDs.Count > MaxTargets ? MaxTargets : newTargetIDs.Count));
      }
      
      // If not in idle box, no collision
      
      projectile.rotation = projectile.velocity.X * 0.05f;
      SelectFrame();
      CreateDust();
      // Orient minion based on movement direction (Commented out bc Sein)
      // if (projectile.velocity.X > 0f) {
      //   projectile.spriteDirection = (projectile.direction = -1);
      // }
      // else if (projectile.velocity.X < 0f) {
      //   projectile.spriteDirection = (projectile.direction = 1);
      // }
      bool attemptFire = (Autoshoot && targeting) || (!Autoshoot && PlayerInput.Triggers.JustPressed.MouseLeft && !Main.LocalPlayer.mouseInterface);
      // Manage Cooldown
      float minCooldown = MinCooldown * (Autoshoot ? 1.5f : 1);
      float shortCooldown = ShortCooldown * (Autoshoot ? 1.5f : 1);
      float longCooldown = LongCooldown * (Autoshoot ? 2f : 1);
      if (Cooldown > 0) { // If on cooldown, increase cooldown
        Cooldown += 1;
        if (Cooldown > longCooldown) {
          Cooldown = 0;
          currShots = 1;
        }
      }
      if (attemptFire && Cooldown > minCooldown && currShots < MaxShotsPerBurst) {
        currShots = Cooldown > shortCooldown ? 1 : currShots + 1;
        Cooldown = 0;
      }
      // If autoshoot
      if (Cooldown == 0 && attemptFire) { // Can fire
        // Orient minion based on target direction (Commnted out bc Sein)
        // if ((targetPos - projectile.Center).X > 0f) {
        //   projectile.spriteDirection = (projectile.direction = -1);
        // } else if ((targetPos - projectile.Center).X < 0f) {
        //   projectile.spriteDirection = (projectile.direction = 1);
        // }

        Cooldown = 1;
        if (Main.myPlayer == projectile.owner) { // Fire
          PlaySpiritFlameSound("Throw" + SpiritFlameSound + OriPlayer.RandomChar(3, ref excludeRand), 0.6f);
          if (!targeting) {
            int i = 0;
            while (i < ShotsToPrimaryTarget) {
              Fire(-1);
              i++;
            }
            return;
          }
          int usedShots = 0;
          int loopCount = 0;
          while (usedShots < MaxShotsPerVolley && loopCount < ShotsToPrimaryTarget) {
            for (int t = 0; t < targetIDs.Count; t++) {
              if (loopCount < (t == 0 ? ShotsToPrimaryTarget : ShotsToTarget)) {
                Fire(t);
                usedShots++;
              }
            }
            loopCount ++;
          }
          projectile.netUpdate = true;
        }
      }
      // Main.NewText("Sein Position: [" + (int)projectile.position.X + ", " + (int)projectile.position.Y + "]");
    }
    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough) {
      fallThrough = true;
      width = 4;
      height = 4;
      return false;
    }
    public override void PostDraw(SpriteBatch spriteBatch, Color lightColor) {
      Texture2D texture = mod.GetTexture("Projectiles/Minions/Sein_Glow");
      int t = (int)(Main.time % 45);
      int frame = t < 15 ? 0 : t < 30 ? 1 : 2;
      spriteBatch.Draw(
        texture,
        new Vector2(
          projectile.Center.X - Main.screenPosition.X + projectile.width * 0.5f,
          projectile.Center.Y - Main.screenPosition.Y + projectile.height * 0.5f
        ),
        new Rectangle(0, frame * texture.Height / 3, texture.Width, texture.Width),
        Color.White,
        projectile.rotation,
        new Vector2(texture.Width, texture.Width) * 0.5f,
        projectile.scale, 
        SpriteEffects.None, 
        0f
      );
    }
  }
}
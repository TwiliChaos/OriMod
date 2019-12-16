using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace OriMod {
  internal static class AnimationHandler {
    private static Frame f(int frameX, int frameY, int duration = -1) => new Frame(frameX, frameY, duration);
    private static Header h(InitType i = InitType.Range, LoopMode l = LoopMode.Always, PlaybackMode p = PlaybackMode.Normal, string s = null)
      => new Header(init: i, loop: l, playback: p, overrideTexturePath: s);

    internal static readonly AnimationSource PlayerAnim = new AnimationSource("PlayerEffects/OriPlayer", 128, 128,
      new Dictionary<string, Track> {
        ["Default"] = new Track(h(),
          f(0, 0)
        ),
        ["Idle"] = new Track(h(),
          f(0, 1, 9), f(0, 8, 9)
        ),
        ["IdleAgainst"] = new Track(h(),
          f(0, 9, 7), f(0, 14, 7)
        ),
        ["LookUpStart"] = new Track(h(),
          f(1, 0)
        ),
        ["LookUp"] = new Track(h(),
          f(1, 1, 8), f(1, 7, 8)
        ),
        ["CrouchStart"] = new Track(h(),
          f(1, 8)
        ),
        ["Crouch"] = new Track(h(),
          f(1, 9)
        ),
        ["Running"] = new Track(h(),
          f(2, 0, 4), f(2, 10, 4)
        ),
        ["Dash"] = new Track(h(i: InitType.Select, l: LoopMode.Once),
          f(2, 12, 36), f(2, 13, 12)
        ),
        ["Bash"] = new Track(h(i: InitType.Select, l: LoopMode.Once),
          f(2, 14, 40), f(2, 13)
        ),
        ["AirJump"] = new Track(h(),
          f(3, 0, 32)
        ),
        ["Jump"] = new Track(h(i: InitType.Select, p: PlaybackMode.Reverse),
          f(3, 1), f(3, 2, 14)
        ),
        ["IntoJumpBall"] = new Track(h(i: InitType.Select, l: LoopMode.Once),
          f(3, 3, 6), f(3, 4, 4)
        ),
        ["ChargeJump"] = new Track(h(l: LoopMode.Once, p: PlaybackMode.PingPong),
          f(3, 5, 4), f(3, 8, 4)
        ),
        ["Falling"] = new Track(h(),
          f(3, 9, 4), f(3, 12, 4)
        ),
        ["FallNoAnim"] = new Track(h(),
          f(3, 13)
        ),
        ["GlideStart"] = new Track(h(l: LoopMode.Once),
          f(4, 0, 5), f(4, 2, 5)
        ),
        ["GlideIdle"] = new Track(h(),
          f(4, 3)
        ),
        ["Glide"] = new Track(h(),
          f(4, 4, 5), f(4, 9, 5)
        ),
        ["ClimbIdle"] = new Track(h(),
          f(5, 0)
        ),
        ["Climb"] = new Track(h(),
          f(5, 1, 4), f(5, 8, 4)
        ),
        ["WallSlide"] = new Track(h(),
          f(5, 9, 5), f(5, 12, 5)
        ),
        ["WallJump"] = new Track(h(),
          f(5, 15, 12)
        ),
        ["WallChargeJumpCharge"] = new Track(h(),
          f(6, 0, 16), f(6, 1, 10), f(6, 2)
        ),
        ["WallChargeJumpAim"] = new Track(h(),
          f(6, 2), f(6, 6)
        ),
        ["Burrow"] = new Track(h(i: InitType.Range),
          f(7, 0, 3), f(7, 7, 3)
        ),
        ["TransformStart"] = new Track(h(i: InitType.Select, l: LoopMode.Transfer, s: "PlayerEffects/Transform"),
          f(0, 0, 2), f(0, 1, 60), f(0, 2, 60), f(0, 3, 120),
          f(0, 4, 40), f(0, 5, 40), f(0, 6, 40), f(0, 7, 30)
        ),
        ["TransformEnd"] = new Track(h(i: InitType.Select),
          f(15, 8, 6), f(15, 9, 50), f(15, 10, 6), f(15, 11, 60),
          f(15, 12, 10), f(15, 13, 40), f(15, 14, 3), f(15, 15, 60)
        ),
      }
    );

    internal static readonly AnimationSource BashAnim = new AnimationSource("PlayerEffects/BashArrow", 152, 20,
      new Dictionary<string, Track> {
        {"Bash", new Track(h(i:InitType.Select),
          f(0, 0)
        )}
      }
    );

    internal static readonly AnimationSource GlideAnim = new AnimationSource("PlayerEffects/Feather", 128, 128,
      new Dictionary<string, Track> {
        {"GlideStart", new Track(h(l:LoopMode.Once),
          f(0, 0, 5), f(0, 2, 5)
        )},
        {"GlideIdle", new Track(h(),
          f(0, 3)
        )},
        {"Glide", new Track(h(),
          f(0, 4, 5), f(0, 9, 5)
        )},
      }
    );

    private static Header OverrideHeader = Header.Default;

    internal static void IncrementFrame(OriPlayer oPlayer, string anim = "Default", int overrideFrame = 0, float overrideTime = 0, int overrideDur = 0, Header overrideHeader = null, Vector2 drawOffset = new Vector2(), float rotDegrees = 0) {
      if (oPlayer == null) {
        return;
      }

      if (overrideHeader == null) {
        overrideHeader = PlayerAnim[anim].Header;
      }

      float rotRads = (float)(rotDegrees / 180 * Math.PI);
      if (!PlayerAnim.TrackNames.Contains(anim)) {
        if (anim != null && anim.Length > 0) {
          OriMod.ErrorFormat("BadTrack", args: anim);
        }
        anim = "Default";
        Track track = PlayerAnim[anim];
        oPlayer.AnimReversed = false;
        oPlayer.SetFrame(anim, 1, overrideTime, track.Frames[0], rotRads);
        return;
      }
      Frame[] frames = PlayerAnim[anim].Frames;
      Header header = PlayerAnim[anim].Header.CopySome(overrideHeader); // X is incrementType (no reason to be used in IncrementFrame()), Y is loopMode, Z is playbackMode
      if (anim != oPlayer.AnimName) {
        OverrideHeader = Header.Default;
      }
      if (overrideHeader != Header.None) {
        OverrideHeader = overrideHeader;
        header = overrideHeader;
      }
      if (OverrideHeader != Header.None && anim == oPlayer.AnimName) {
        header = OverrideHeader;
      }
      Frame newFrame;
      if (overrideFrame != -1 && overrideFrame < frames.Length) { // If override frame, just set frame
        newFrame = frames[overrideFrame];
        oPlayer.AnimReversed = header.Playback == PlaybackMode.Reverse;
        oPlayer.SetFrame(anim, overrideFrame, 0, newFrame, rotRads);
      }
      else { // Else actually do work
        int frameIndex = oPlayer.AnimIndex; // frameIndex's lowest value is 1, as frames[0] contains header data for the track
        float time = overrideTime != 0 ? overrideTime : oPlayer.AnimTime;
        Point currFrame = oPlayer.AnimTile;

        if (anim == oPlayer.AnimName) {
          int testFrame = Array.FindIndex(frames, f => f.Tile == currFrame); // Check if this frame already exists
          if (testFrame == -1) {
            OriMod.ErrorFormat("BadFrame", args: new object[] { anim, currFrame });
            frameIndex = header.Playback == PlaybackMode.Reverse ? frames.Length - 1 : 0;
          }
        }
        else {
          frameIndex = header.Playback == PlaybackMode.Reverse ? frames.Length - 1 : 0;
          time = 0;
        }
        int dur = overrideDur != 0 ? overrideDur : frames[frameIndex].Duration;
        int framesToAdvance = 0;
        while (time > dur && dur != -1) {
          time -= dur;
          framesToAdvance++;
          if (framesToAdvance + frameIndex > frames.Length - 1) {
            time %= dur;
          }
        }
        if (framesToAdvance != 0) {
          if (header.Playback == PlaybackMode.Normal) {
            oPlayer.AnimReversed = false;
            if (frameIndex == frames.Length - 1) {
              if (header.Loop == LoopMode.Transfer) {
                anim = header.TransferTo;
                frameIndex = 0;
                time = 0;
              }
              else if (header.Loop != LoopMode.Once) {
                frameIndex = 0;
              }
            }
            else {
              frameIndex += framesToAdvance;
              if (frameIndex > frames.Length - 1) {
                frameIndex = frames.Length - 1;
              }
            }
          }
          else if (header.Playback == PlaybackMode.PingPong) {
            if (frameIndex == 0 && header.Loop != LoopMode.Once) {
              oPlayer.AnimReversed = false;
              frameIndex += framesToAdvance;
              if (frameIndex > frames.Length - 1) {
                frameIndex = frames.Length - 1;
              }
            }
            else if (frameIndex == frames.Length - 1 && header.Loop != LoopMode.Once) {
              oPlayer.AnimReversed = true;
              frameIndex -= framesToAdvance;
              if (frameIndex < 0) {
                frameIndex = 0;
              }
            }
            else {
              frameIndex += oPlayer.AnimReversed ? -framesToAdvance : framesToAdvance;
              if (frameIndex > frames.Length - 1) {
                frameIndex = frames.Length - 1;
              }
              else if (frameIndex < 0) {
                frameIndex = 0;
              }
            }
          }
          else if (header.Playback == PlaybackMode.Reverse) {
            oPlayer.AnimReversed = true;
            if (frameIndex == 0) {
              if (header.Loop != LoopMode.Once) {
                frameIndex = frames.Length - 1;
              }
            }
            else {
              frameIndex -= framesToAdvance;
              if (frameIndex < 0) {
                frameIndex = 0;
              }
            }
          }
        }
        newFrame = frames[frameIndex];
        oPlayer.SetFrame(anim, frameIndex, time, newFrame, rotRads);
      }
    }
  }

  internal enum InitType {
    None = 0,
    Range = 1,
    Select = 2,
  }
  internal enum LoopMode {
    None = 0,
    Always = 1,
    Once = 2,
    Transfer = 3,
  }
  internal enum PlaybackMode {
    None = 0,
    Normal = 1,
    PingPong = 2,
    Reverse = 3,
    Random = 4,
  }

  internal class Header : IDisposable {
    internal InitType Init;
    internal LoopMode Loop;
    internal PlaybackMode Playback;
    internal Texture2D Texture => !_tex?.IsDisposed ?? false ? _tex : (TexturePath != null ? _tex = OriMod.Instance.GetTexture(TexturePath) : null);
    private Texture2D _tex;
    private string TexturePath;
    internal string TransferTo { get; private set; }

    internal Header(InitType init = InitType.None, LoopMode loop = LoopMode.None, PlaybackMode playback = PlaybackMode.None, string transferTo = null, string overrideTexturePath = null) {
      Init = init;
      Loop = loop;
      Playback = playback;
      TexturePath = overrideTexturePath;
    }

    internal Header CopySome(Header other) {
      return new Header(
        other.Init != 0 ? other.Init : Init,
        other.Loop != 0 ? other.Loop : Loop,
        other.Playback != 0 ? other.Playback : Playback
      );
    }

    internal static Header Default => new Header(InitType.Range, LoopMode.Always, PlaybackMode.Normal);
    internal static Header None => new Header(InitType.None, LoopMode.None, PlaybackMode.None);
    public override string ToString()
      => $"Init: {Init} | Loop: {Loop} | Playback: {Playback}" + (Texture != null ? $" | Texture Path: \"{Texture.Name}\"" : "");

    public void Dispose() {
      if (_tex != null) {
        _tex.Dispose();
        _tex = null;
      }
      TexturePath = null;
      TransferTo = null;
    }
  }

  internal class Frame {
    internal readonly byte X;
    internal readonly byte Y;
    internal Point Tile => new Point(X, Y);
    internal int Duration;

    internal Frame(int x, int y, int duration = -1) : this((byte)x, (byte)y, duration) { }
    internal Frame(byte x, byte y, int duration = -1) {
      X = x;
      Y = y;
      Duration = duration;
    }

    public override string ToString() => $"Tile [{X}, {Y}] Duration {Duration}";
  }

  internal class Track : IDisposable {
    internal Header Header { get; private set; }
    internal Frame[] Frames { get; private set; }
    internal int Duration { get; }

    internal Track(Header header, params Frame[] frames) {
      Header = header;

      if (header.Init != InitType.Range || frames.Length < 2) {
        Frames = frames;
      }
      else {
        var newFrames = new List<Frame>();
        for (int i = 0; i < frames.Length - 1; i++) {
          Frame startFrame = frames[i];
          Frame endFrame = frames[i + 1];
          for (int y = startFrame.Y; y < endFrame.Y; y++) {
            newFrames.Add(new Frame(startFrame.X, y, startFrame.Duration));
          }
        }
        newFrames.Add(frames[frames.Length - 1]);
        Frames = newFrames.ToArray();
      }

      foreach (Frame f in frames) {
        if (f.Duration == -1) {
          Duration = -1;
          break;
        }
        Duration += f.Duration;
      }
    }

    public void Dispose() {
      Header.Dispose();
      Header = null;
      Frames = null;
    }
  }

  internal class AnimationSource : IDisposable {
    internal Dictionary<string, Track> Tracks { get; private set; }
    internal Point TileSize { get; }
    internal Texture2D Texture => !_tex?.IsDisposed ?? false ? _tex : (_tex = OriMod.Instance.GetTexture(texturePath));
    private Texture2D _tex;
    private readonly string texturePath;
    internal string[] TrackNames { get; private set; }
    internal Track this[string name] => Tracks[name];

    internal AnimationSource(string texture, int x, int y, Dictionary<string, Track> tracks) {
      texturePath = texture;
      Tracks = tracks;
      TrackNames = tracks.Keys.ToArray();
      TileSize = new Point(x, y);
    }

    public void Dispose() {
      if (Tracks != null) {
        foreach (var track in Tracks.Values) {
          track.Dispose();
        }
        Tracks = null;
      }
      if (_tex != null) {
        _tex?.Dispose();
        _tex = null;
      }
      TrackNames = null;
    }
  }

  internal class Animation : IDisposable {
    internal Texture2D Texture => ActiveTrack.Header.Texture ?? Source.Texture;
    internal PlayerLayer PlayerLayer { get; private set; }
    internal bool Valid { get; private set; }
    internal Track ActiveTrack => Valid ? Source.Tracks[Handler.owner.AnimName] : Source.Tracks.First().Value;
    internal Frame ActiveFrame => ActiveTrack.Frames[Handler.owner.AnimIndex < ActiveTrack.Frames.Length ? Handler.owner.AnimIndex : 0];
    internal Rectangle ActiveTile => new Rectangle(ActiveFrame.Tile.X * Source.TileSize.X, ActiveFrame.Tile.Y * Source.TileSize.Y, Source.TileSize.X, Source.TileSize.Y);
    internal Animations Handler { get; private set; }
    internal AnimationSource Source { get; private set; }

    internal Animation(Animations handler, AnimationSource source, PlayerLayer playerLayer) {
      Handler = handler;
      Source = source;
      PlayerLayer = playerLayer;
    }

    internal void InsertInLayers(List<PlayerLayer> layers, int idx = 0, bool force = false) {
      if (Valid || force) {
        layers.Insert(idx, this.PlayerLayer);
      }
    }
    /// <summary> Add the PlayerLayer of this animation to the given `layers`
    /// 
    /// This will not run if Valid is false, unless force is true </summary>
    /// <param name="layers">The PlayerLayer list to add to</param>
    /// <param name="force">Add this Player even if Valid is false </param>
    internal void AddToLayers(List<PlayerLayer> layers, bool force = false) {
      if (Valid || force) {
        layers.Add(this.PlayerLayer);
      }
    }

    internal void OnAnimNameChange(string name) => Valid = Source.Tracks.ContainsKey(name);

    public void Dispose() {
      PlayerLayer = null;
      Handler = null;
      Source.Dispose();
      Source = null;
    }
  }

  internal class Animations {
    internal OriPlayer owner { get; private set; }
    internal Animation PlayerAnim { get; private set; }
    internal Animation SecondaryLayer { get; private set; }
    internal Animation TrailAnim { get; private set; }
    internal Animation BashAnim { get; private set; }
    internal Animation GlideAnim { get; private set; }
    internal Animations(OriPlayer oPlayer) {
      owner = oPlayer;
      PlayerAnim = new Animation(this, AnimationHandler.PlayerAnim, OriLayers.PlayerSprite);
      SecondaryLayer = new Animation(this, AnimationHandler.PlayerAnim, OriLayers.SecondaryLayer);
      TrailAnim = new Animation(this, AnimationHandler.PlayerAnim, OriLayers.Trail);
      BashAnim = new Animation(this, AnimationHandler.BashAnim, OriLayers.BashArrow);
      GlideAnim = new Animation(this, AnimationHandler.GlideAnim, OriLayers.FeatherSprite);
    }

    internal void Dispose() {
      owner = null;
      PlayerAnim.Dispose();
      SecondaryLayer.Dispose();
      TrailAnim.Dispose();
      BashAnim.Dispose();
      GlideAnim.Dispose();
      PlayerAnim = SecondaryLayer = TrailAnim = BashAnim = GlideAnim = null;
    }
  }
}

namespace Babylon
{
    /// <summary>Every generated sound effect. File overrides use the same name (e.g. Audio/Override/BossRoar.wav).</summary>
    public enum SoundId
    {
        None = 0,
        DesertWind, TorchCrackle, Footstep, SwordWhoosh, HitThud, StoneClang, SpiderHiss, SpiderSkitter,
        KeyJingle, HealShimmer, Chime, WrongBuzz, DartWhoosh, DoorGrind, BossRoar, GroundSmash, Crumble,
        VictoryFanfare, UIClick, ScorpionSting, PlayerHurt, Land, Roll, BossStep, Rumble, SwordShing,
        Throw, CaveDrips
    }

    /// <summary>Generative Hijaz-scale music loops.</summary>
    public enum MusicId { None = 0, Calm, Tense, Mysterious, Intense, Triumphant }
}

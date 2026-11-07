using UnityEngine.Timeline;

namespace Editor
{
    /// <summary>每个技能效果对应一条 Timeline 轨道。</summary>
    [TrackClipType(typeof(SkillEffectTimelineClip))]
    [TrackColor(0.28f, 0.55f, 0.72f)]
    public sealed class SkillEffectTimelineTrack : TrackAsset
    {
    }
}

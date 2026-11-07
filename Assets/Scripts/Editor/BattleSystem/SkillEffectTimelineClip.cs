using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Editor
{
    /// <summary>技能编辑器的临时 Timeline 片段，不参与运行时播放。</summary>
    public sealed class SkillEffectTimelineClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Null;
        }
    }
}

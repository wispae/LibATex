using LibATex.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace LibATex.Util
{
    public class TimeAnimatedTexture : AnimatedTexture
    {
        private float dayPercentagePerFrame = 1.0f;

        public float DayPercentagePerFrame
        {
            get
            {
                return dayPercentagePerFrame;
            }
            set
            {
                if (value <= 0)
                {
                    dayPercentagePerFrame = 1.0f;
                }

                dayPercentagePerFrame = value;
            }
        }

        public float TimeOffset { get; set; }
        public float TimeMultiplier { get; set; }

        public TimeAnimatedTexture(ICoreClientAPI capi) : base(capi) { }

        public TimeAnimatedTexture(ICoreClientAPI capi, ITextureAtlasAPI animatedTextureAtlas, AssetLocation sourceLocation, AssetLocation targetLocation, AnimatedTextureConfig configuration) : base(capi, animatedTextureAtlas, sourceLocation, targetLocation, configuration)
        {
            dayPercentagePerFrame = 1.0f / NumFrames;
            TimeOffset = configuration.TimeOptions.TimeOffset;
            TimeMultiplier = configuration.TimeOptions.TimeMultiplier;
        }

        public override void Advance(ICoreClientAPI capi, float dayTime, ref bool didRender)
        {
            dayTime = (dayTime * TimeMultiplier + TimeOffset) % 1.0f;
            int nextFrame = (int)MathF.Floor(dayTime / DayPercentagePerFrame);
            if (nextFrame >= NumFrames)
            {
                nextFrame = NumFrames - 1;
            }
            if (nextFrame == CurrentFrame) return;

            currentRow = nextFrame / Rows;
            currentColumn = nextFrame % Columns;

            RenderCurrentFrame(capi);

            didRender = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace LibATex.Util
{
	[Flags]
	public enum EnumAnimatedTextureType
	{
		AnimatedTexture = 0x01,
		PartialAnimatedTexture = 0x02,
		PeriodicAnimatedTexture = 0x04,
		PeriodicRandomAnimatedTexture = 0x08,
		RandomAnimatedTexture = 0x10,
		TimeAnimatedTexture = 0x20,
		RandomPeriodicAnimatedTexture = PeriodicAnimatedTexture | RandomAnimatedTexture
	}

	public interface IAnimatedTexture
	{
        public ulong Id { get; }
        public bool IsActive { get; }
        public bool IsComplete { get; }
        public int NumFrames { get; set; }
        public int CurrentFrame { get; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int TargetOffsetX { get; set; }
        public int TargetOffsetY { get; set; }
        /// <summary>
        /// The total height of the animated texture in pixels
        /// </summary>
        public int PixelHeight { get; }
        /// <summary>
        /// The total width of the animated texture in pixels
        /// </summary>
        public int PixelWidth { get; }

        public float TimePerFrame { get; set; }
        public void Advance(ICoreClientAPI capi, float time, ref bool didRender);
		public void NextFrame(ICoreClientAPI capi);
		public void RenderCurrentFrame(ICoreClientAPI capi);
	}
}

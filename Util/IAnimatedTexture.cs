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
		public void Advance(ICoreClientAPI api, float dt, ref bool didRender);
		public void NextFrame(ICoreClientAPI capi);
		public void RenderCurrentFrame(ICoreClientAPI capi);
	}
}

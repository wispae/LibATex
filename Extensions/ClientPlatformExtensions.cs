using System.Reflection;

namespace LibATex.Extensions
{
	using Vintagestory.Client.NoObf;
	using OpenTK.Graphics.OpenGL;
	using Vintagestory.API.Client;
	using System;
	using System.Threading;
	using Vintagestory.API.Config;
	using Vintagestory.API.Common;
	using LibATex.Util;

	public static class ClientMainExtensions
	{
		/// <summary>
		/// Creates an arbitrary framebuffer, unmanaged by the main game<br/>
		/// Remember to destroy the FB when no longer needed!
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static int CreateBlankFramebuffer(this ClientMain self)
		{
			int bufferId;
			GL.GenFramebuffers(1, out bufferId);
			return bufferId;
		}

		/// <summary>
		/// IMPORTANT: This allows you to destroy any arbitrary framebuffer, incorrect use WILL fuck up the game
		/// </summary>
		/// <remarks>
		/// Consider yourself warned
		/// </remarks>
		/// <param name="self"></param>
		/// <param name="bufferId"></param>
		public static void DestroyBlankFramebuffer(this ClientMain self, int bufferId)
		{
			GL.DeleteFramebuffer(bufferId);
		}

		/// <summary>
		/// Renders a region from one texture into another, using temporary framebuffers
		/// that are disposed of before returning
		/// </summary>
		/// <remarks>
		/// I'm not really sure of the performance impact of generating and then disposing
		/// of framebuffers potentially multiple times per frame, so preferable to use the other one
		/// </remarks>
		/// <param name="self"></param>
		/// <param name="fromTextureId"></param>
		/// <param name="intoTextureId"></param>
		/// <param name="sourceX1"></param>
		/// <param name="sourceY1"></param>
		/// <param name="sourceX2"></param>
		/// <param name="sourceY2"></param>
		/// <param name="targetX1"></param>
		/// <param name="targetY1"></param>
		/// <param name="targetX2"></param>
		/// <param name="targetY2"></param>
		public static void BlitTextureIntoTexture(this ClientMain self, int fromTextureId, int intoTextureId, int sourceX1, int sourceY1, int sourceX2, int sourceY2, int targetX1, int targetY1, int targetX2, int targetY2)
		{
			int originBufferId;
			GL.GetInteger(GetPName.FramebufferBinding, out originBufferId);

			int fromBufferId, intoBufferId;
			GL.GenFramebuffers(1, out fromBufferId);
			GL.GenFramebuffers(1, out intoBufferId);

			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fromBufferId);
			GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, fromTextureId, 0);

			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, intoBufferId);
			GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, intoTextureId, 0);

			GL.BlitFramebuffer(sourceX1, sourceY1, sourceX2, sourceY2, targetX1, targetY1, targetX2, targetY2, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

			GL.DeleteFramebuffer(fromBufferId);
			GL.DeleteFramebuffer(intoBufferId);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, originBufferId);
		}

		/// <summary>
		/// Renders a region from one texture into another, using the framebuffers provided
		/// </summary>
		/// <param name="self"></param>
		/// <param name="fromTextureId"></param>
		/// <param name="intoTextureId"></param>
		/// <param name="sourceX1"></param>
		/// <param name="sourceY1"></param>
		/// <param name="sourceX2"></param>
		/// <param name="sourceY2"></param>
		/// <param name="targetX1"></param>
		/// <param name="targetY1"></param>
		/// <param name="targetX2"></param>
		/// <param name="targetY2"></param>
		/// <param name="readBufferId"></param>
		/// <param name="drawBufferId"></param>
		public static void BlitTextureIntoTexture(this ClientMain self, int fromTextureId, int intoTextureId, int sourceX1, int sourceY1, int sourceX2, int sourceY2, int targetX1, int targetY1, int targetX2, int targetY2, int readBufferId, int drawBufferId)
		{
			int originBufferId;
			GL.GetInteger(GetPName.FramebufferBinding, out originBufferId);

			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readBufferId);
			GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, fromTextureId, 0);

			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawBufferId);
			GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, intoTextureId, 0);

			GL.BlitFramebuffer(sourceX1, sourceY1, sourceX2, sourceY2, targetX1, targetY1, targetX2, targetY2, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, originBufferId);
		}

		public static void BlitFramebufferIntoFramebuffer(this ClientMain self, int fromBufferId, int intoBufferId, int sourceX1, int sourceY1, int sourceX2, int sourceY2, int targetX1, int targetY1, int targetX2, int targetY2)
		{
			int originBufferId;

			GL.GetInteger(GetPName.FramebufferBinding, out originBufferId);

			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fromBufferId);
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, intoBufferId);

			GL.BlitFramebuffer(sourceX1, sourceY1, sourceX2, sourceY2, targetX1, targetY1, targetX2, targetY2, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, originBufferId);
		}

		public static void BlitFramebufferIntoTexture(this ClientMain self, int fromBufferId, int intoTextureId, int sourceX1, int sourceY1, int sourceX2, int sourceY2, int targetX1, int targetY1, int targetX2, int targetY2)
		{
			int originBufferId;

			GL.GetInteger(GetPName.FramebufferBinding, out originBufferId);

			int intoBufferId;
			GL.GenFramebuffers(1, out intoBufferId);

			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fromBufferId);

			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, intoBufferId);
			GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, intoTextureId, 0);

			GL.BlitFramebuffer(sourceX1, sourceY1, sourceX2, sourceY2, targetX1, targetY1, targetX2, targetY2, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

			GL.DeleteFramebuffer(fromBufferId);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, originBufferId);
		}
	}

	public static class TextureAtlasManagerExtensions
	{
		public static void RenderTextureIntoAtlasPersistent(this ITextureAtlasAPI self, ICoreClientAPI api, int atlasTextureNumber, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, int offsetX = 0, int offsetY = 0)
		{
			if (Thread.CurrentThread.ManagedThreadId != RuntimeEnv.MainThreadId)
			{
				throw new InvalidOperationException("Attempting to blit a texture into the atlas outside of the main thread. This is not possible as we have only one OpenGL context!");
			}

            ClientMain game = api.World as ClientMain;

			LoadedTexture atlasTexture = self.AtlasTextures[atlasTextureNumber];

			int srcX = (int)MathF.Round(fromTexture.Width * sourceX);
			int srcY = (int)MathF.Round(fromTexture.Height * sourceY);
			int srcW = (int)MathF.Round(fromTexture.Width * sourceWidth);
			int srcH = (int)MathF.Round(fromTexture.Height * sourceHeight);

			int dstX = (int)MathF.Round(atlasTexture.Width * targetX) + offsetX;
			int dstY = (int)MathF.Round(atlasTexture.Height * targetY) + offsetY;

			if (self is AnimatedTextureAtlasManager)
			{
				int readBuf = (self as AnimatedTextureAtlasManager).readBufferRef.FboId;
				int drawBuf = (self as AnimatedTextureAtlasManager).drawBufferRef.FboId;
				game.BlitTextureIntoTexture(fromTexture.TextureId, atlasTexture.TextureId, srcX, srcY, srcX + srcW, srcY + srcH, dstX, dstY, dstX + srcW, dstY + srcH, readBuf, drawBuf);
			} else
			{
				game.BlitTextureIntoTexture(fromTexture.TextureId, atlasTexture.TextureId, srcX, srcY, srcX + srcW, srcY + srcH, dstX, dstY, dstX + srcW, dstY + srcH);
			}
		}

		public static bool ContainsAssetLocation(this ITextureAtlasAPI self, AssetLocation location)
		{
			return (self as TextureAtlasManager).ContainsKey(location);
		}
	}
}

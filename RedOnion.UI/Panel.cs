using System;
using UnityEngine;
using UUI = UnityEngine.UI;

namespace RedOnion.UI
{
	public class Panel : Element
	{
		protected UUI.RawImage image;
		protected UUI.RawImage Image
		{
			get
			{
				if (image == null)
				{
					if (GameObject == null)
						throw new ObjectDisposedException(Name ?? GetType().Name);
					image = GameObject.AddComponent<UUI.RawImage>();
				}
				return image;
			}
		}

		public Panel(string name = null)
			: base(name)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing || GameObject == null)
				return;
			base.Dispose(disposing);
			image = null;
		}

		public Color Color
		{
			get => image?.color ?? new Color();
			set => Image.color = value;
		}
		public Texture Texture
		{
			get => image?.texture;
			set => Image.texture = value;
		}
	}
}

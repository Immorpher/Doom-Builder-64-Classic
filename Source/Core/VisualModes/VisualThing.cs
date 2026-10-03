
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.ComponentModel;
using CodeImp.DoomBuilder.Map;
using SlimDX.Direct3D9;
using SlimDX;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing.Imaging;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Rendering;

#endregion

namespace CodeImp.DoomBuilder.VisualModes
{
	public abstract class VisualThing : IVisualPickable, ID3DResource, IComparable<VisualThing>
	{
		#region ================== Constants
		
		#endregion
		
		#region ================== Variables
		
		// Thing
		private Thing thing;
		
		// Textures (one for each sprite rotation; a single one when the sprite has no rotations)
		private ImageData[] textures;
		
		// Geometry (one set for each sprite rotation)
		private WorldVertex[][] vertices;
		private VertexBuffer[] geobuffers;
		private bool updategeo;
		private int[] triangles;
		private int spriteframe;	// Sprite rotation currently shown
		
		// Rendering
		private int renderpass;
		private Matrix orientation;
		private Matrix position;
		private Matrix cagescales;
		private bool billboard;
		private Vector2D pos2d;
		private float cameradistance;
		private int cagecolor;

		// Selected?
		protected bool selected;

		// Disposing
		private bool isdisposed = false;
		
		#endregion
		
		#region ================== Properties
		
		internal VertexBuffer GeometryBuffer { get { return ((geobuffers != null) && (spriteframe < geobuffers.Length)) ? geobuffers[spriteframe] : null; } }
		internal bool NeedsUpdateGeo { get { return updategeo; } }
		internal int Triangles { get { return ((triangles != null) && (spriteframe < triangles.Length)) ? triangles[spriteframe] : 0; } }
		internal int RenderPassInt { get { return renderpass; } }
		internal Matrix Orientation { get { return orientation; } }
		internal Matrix Position { get { return position; } }
		internal Matrix CageScales { get { return cagescales; } }
		internal int CageColor { get { return cagecolor; } }

		/// <summary>
		/// Set to True to use billboarding for this thing. When using billboarding,
		/// the geometry will be rotated on the XY plane to face the camera.
		/// </summary>
		public bool Billboard { get { return billboard; } set { billboard = value; } }

		/// <summary>
		/// Returns the Thing that this VisualThing is created for.
		/// </summary>
		public Thing Thing { get { return thing; } }

		/// <summary>
		/// Render pass in which this geometry must be rendered. Default is Solid.
		/// </summary>
		public RenderPass RenderPass { get { return (RenderPass)renderpass; } set { renderpass = (int)value; } }
		
		/// <summary>
		/// Image to use as texture on the geometry.
		/// </summary>
		public ImageData Texture
		{
			get { return ((textures != null) && (spriteframe < textures.Length)) ? textures[spriteframe] : null; }
			set { textures = new ImageData[] { value }; spriteframe = 0; }
		}

		/// <summary>
		/// Disposed or not?
		/// </summary>
		public bool IsDisposed { get { return isdisposed; } }

		/// <summary>
		/// Selected or not? This is only used by the core to determine what color to draw it with.
		/// </summary>
		public bool Selected { get { return selected; } set { selected = value; } }
		
		#endregion
		
		#region ================== Constructor / Destructor
		
		// Constructor
		public VisualThing(Thing t)
		{
			// Initialize
			this.thing = t;
			this.renderpass = (int)RenderPass.Mask;
			this.billboard = true;
			this.orientation = Matrix.Identity;
			this.position = Matrix.Identity;
			this.cagescales = Matrix.Identity;
			
			// Register as resource
			General.Map.Graphics.RegisterResource(this);
		}

		// Disposer
		public virtual void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up
				DisposeBuffers();

				// Unregister resource
				General.Map.Graphics.UnregisterResource(this);

				// Done
				isdisposed = true;
			}
		}
		
		#endregion
		
		#region ================== Methods
		
		// This sets the distance from the camera
		internal void CalculateCameraDistance(Vector2D campos)
		{
			cameradistance = Vector2D.DistanceSq(pos2d, campos);
		}
		
		// This is called before a device is reset
		// (when resized or display adapter was changed)
		public void UnloadResource()
		{
			// Trash geometry buffers
			DisposeBuffers();
			updategeo = true;
		}

		// This disposes all geometry buffers
		private void DisposeBuffers()
		{
			if(geobuffers != null)
			{
				foreach(VertexBuffer buffer in geobuffers)
					if(buffer != null) buffer.Dispose();
			}
			geobuffers = null;
		}
		
		// This is called resets when the device is reset
		// (when resized or display adapter was changed)
		public void ReloadResource()
		{
			// Make new geometry
			//Update();
		}

		/// <summary>
		/// Sets the size of the cage around the thing geometry.
		/// </summary>
		public void SetCageSize(float radius, float height)
		{
			cagescales = Matrix.Scaling(radius, radius, height);
		}

		/// <summary>
		/// Sets the color of the cage around the thing geometry.
		/// </summary>
		public void SetCageColor(PixelColor color)
		{
			cagecolor = color.ToInt();
		}

		/// <summary>
		/// This sets the position to use for the thing geometry.
		/// </summary>
		public void SetPosition(Vector3D pos)
		{
			pos2d = new Vector2D(pos);
			position = Matrix.Translation(D3DDevice.V3(pos));
		}

		/// <summary>
		/// This sets the orientation to use for the thing geometry. When using this, you may want to turn off billboarding.
		/// </summary>
		public void SetOrientation(Vector3D angles)
		{
			orientation = Matrix.RotationYawPitchRoll(angles.z, angles.y, angles.x);
		}
		
		// This sets the vertices for the thing sprite
		protected void SetVertices(ICollection<WorldVertex> verts)
		{
			// Copy vertices
			WorldVertex[] copy = new WorldVertex[verts.Count];
			verts.CopyTo(copy, 0);
			SetFrameVertices(new WorldVertex[][] { copy });
		}

		/// <summary>
		/// This sets the vertices for each rotation of the thing sprite. This must be used together with
		/// SetFrameTextures and both must have the same number of items (either 1 or 8).
		/// </summary>
		protected void SetFrameVertices(WorldVertex[][] framevertices)
		{
			// Copy vertices
			vertices = new WorldVertex[framevertices.Length][];
			triangles = new int[framevertices.Length];
			for(int i = 0; i < framevertices.Length; i++)
			{
				vertices[i] = new WorldVertex[framevertices[i].Length];
				framevertices[i].CopyTo(vertices[i], 0);
				triangles[i] = vertices[i].Length / 3;
			}

			spriteframe = 0;
			updategeo = true;
		}

		/// <summary>
		/// This sets the texture for each rotation of the thing sprite.
		/// </summary>
		protected void SetFrameTextures(ImageData[] frametextures)
		{
			textures = new ImageData[frametextures.Length];
			frametextures.CopyTo(textures, 0);
			spriteframe = 0;
		}

		// This selects the sprite rotation to show, based on where the camera is
		// in relation to the direction the thing is facing.
		internal void UpdateSpriteFrame()
		{
			if((textures == null) || (vertices == null) || (textures.Length != 8) || (vertices.Length != 8))
			{
				spriteframe = 0;
			}
			else
			{
				// Convert to [0..7] range; 292 == 270 + 45/2
				Vector3D camdelta = General.Map.VisualCamera.Position - thing.Position;
				spriteframe = General.ClampAngle((int)Angle2D.RadToDeg(camdelta.GetAngleXY()) - thing.AngleDoom + 292) / 45;
			}
		}
		
		// This updates the visual thing
		public virtual void Update()
		{
			// Do we need to update the geometry buffer?
			if(updategeo)
			{
				// Trash geometry buffers
				DisposeBuffers();

				// Any vertices?
				if(vertices != null)
				{
					geobuffers = new VertexBuffer[vertices.Length];
					for(int i = 0; i < vertices.Length; i++)
					{
						if(vertices[i].Length > 0)
						{
							// Make a new buffer
							geobuffers[i] = new VertexBuffer(General.Map.Graphics.Device, WorldVertex.Stride * vertices[i].Length,
															 Usage.WriteOnly | Usage.Dynamic, VertexFormat.None, Pool.Default);

							// Fill the buffer
							DataStream bufferstream = geobuffers[i].Lock(0, WorldVertex.Stride * vertices[i].Length, LockFlags.Discard);
							bufferstream.WriteRange<WorldVertex>(vertices[i]);
							geobuffers[i].Unlock();
							bufferstream.Dispose();
						}
					}
				}

				// Done
				updategeo = false;
			}
		}
		
		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should reject
		/// as fast as possible to rule out all geometry that certainly does not touch the line.
		/// </summary>
		public virtual bool PickFastReject(Vector3D from, Vector3D to, Vector3D dir)
		{
			return false;
		}
		
		/// <summary>
		/// This is called when the thing must be tested for line intersection. This should perform
		/// accurate hit detection and set u_ray to the position on the ray where this hits the geometry.
		/// </summary>
		public virtual bool PickAccurate(Vector3D from, Vector3D to, Vector3D dir, ref float u_ray)
		{
			return false;
		}
		
		/// <summary>
		/// This sorts things by distance from the camera. Farthest first.
		/// </summary>
		public int CompareTo(VisualThing other)
		{
			return Math.Sign(other.cameradistance - this.cameradistance);
		}
		
		#endregion
	}
}

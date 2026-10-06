#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.VisualModes
{
	/// <summary>
	/// This class provides the camera in Visual Mode
	/// </summary>
	public class VisualCamera
	{
		#region ================== Constants

		private const float ANGLE_FROM_MOUSE = 0.0001f;
		public const float MAX_ANGLEZ_LOW = 91f / Angle2D.PIDEG;
		public const float MAX_ANGLEZ_HIGH = (360f - 91f) / Angle2D.PIDEG;
		public const float THING_Z_OFFSET = 41.0f;
		
		#endregion

		#region ================== Variables

		// Properties
		private Vector3D position;
		private Vector3D target;
		private Vector3D movemultiplier;
		private float anglexy, anglez;
		private Sector sector;
		
		#endregion

		#region ================== Properties

		public Vector3D Position { get { return position; } set { position = value; } }
		public Vector3D Target { get { return target; } }
		public float AngleXY { get { return anglexy; } set { anglexy = value; } }
		public float AngleZ { get { return anglez; } set { anglez = value; } }
		public Sector Sector { get { return sector; } internal set { sector = value; } }
		public Vector3D MoveMultiplier { get { return movemultiplier; } set { movemultiplier = value; } }
		
		#endregion

		#region ================== Constructor / Destructor

		// Constructor
		public VisualCamera()
		{
			// Initialize
			this.movemultiplier = new Vector3D(1.0f, 1.0f, 1.0f);
			this.anglexy = 0.0f;
			this.anglez = Angle2D.PI;
			this.sector = null;
		}
		
		#endregion

		#region ================== Methods

		// Mouse input
		internal void ProcessMouseInput(Vector2D delta)
		{
			// Change camera angles with the mouse changes
			anglexy -= delta.x * ANGLE_FROM_MOUSE;
			if(General.Settings.InvertYAxis)
				anglez -= delta.y * ANGLE_FROM_MOUSE;
			else
				anglez += delta.y * ANGLE_FROM_MOUSE;

			// Normalize angles
			anglexy = Angle2D.Normalized(anglexy);
			anglez = Angle2D.Normalized(anglez);

			// Limit vertical angle
			if(anglez < MAX_ANGLEZ_LOW) anglez = MAX_ANGLEZ_LOW;
			if(anglez > MAX_ANGLEZ_HIGH) anglez = MAX_ANGLEZ_HIGH;
		}

		// Key input
		internal void ProcessMovement(Vector3D deltavec)
		{
			// Calculate camera direction vectors
			Vector3D camvec = Vector3D.FromAngleXYZ(anglexy, anglez);

			// Position the camera
			position += deltavec;
			
			// Target the camera
			target = position + camvec;
		}

		#endregion
	}
}

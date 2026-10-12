using Godot;

namespace DakotaTalley.OrbitCamera;

// Node Response Table
// Player Does...					| Action	| Changes which node?	| Which property?	| Limits
// Scroll wheel						| Zoom		| Camera3D				| Position Z		| min/max zoom distance
// Middle click + drag horizontal	| Yaw		| PivotCam				| Rotation Y		| none
// Middle click + drag vertical		| Pitch		| Arm					| Rotation X		| -85 - +60 (potential ground limit at 0?)
// Right click + drag h/v			| Pan		| PivotCam				| Position X/Z		| Plot or level extents

public partial class PivotCam : Node3D
{
	public enum DragStyle { GrabWorld, PushCamera }

	private struct CameraState
	{
		public float Distance;
		public float Yaw; // Radians
		public float Pitch; // Radians
		public Vector3 PivotPosition; // Parent space
	}

	[ExportGroup("Zoom")]
	[Export] public float ZoomDistance = 12.0f;
	[Export] public float ZoomMin = 5.0f;
	[Export] public float ZoomMax = 100.0f;
	[Export] public float ZoomFactor = 0.9f;
	[Export] public bool InvertZoom = false;
	[ExportGroup("Orbit")]
	[Export] public float OrbitSensitivity = Mathf.Tau / 1920f;
	[Export(PropertyHint.Range, "-85,-60,1")] public int PitchMin = -85;
	[Export(PropertyHint.Range, "-10,85,1")] public int PitchMax = 60;
	[Export] public bool InvertPitch = false;
	[Export] public bool InvertYaw = false;
	[ExportGroup("Pan")]
	[Export] public float PanSensitivity = 0.001f;
	[Export] public float PanKeySpeed = 1.0f;
	[Export] public DragStyle PanDragStyle = DragStyle.GrabWorld;
	[ExportGroup("References")]
	[Export] public Node3D Arm;
	[Export] public Camera3D Camera;
	[Export] public Node3D BoundingBox;

	private CameraState _startState;
	

	public override void _Ready()
	{
		SetZoom(ZoomDistance);
		_startState = Capture();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("camera_reset"))
		{
			ResetView();
			GetViewport().SetInputAsHandled();
			return;
		}

		switch (@event)
		{
			case InputEventMouseMotion motion:
				HandleMouseMotion(motion);
				break;

			case InputEventMouseButton button:
				HandleMouseButton(button);
				break;
		}

	}

    public override void _Process(double delta)
    {
		Vector2 panVector = Input.GetVector(
			"camera_pan_left", 
			"camera_pan_right", 
			"camera_pan_forward", 
			"camera_pan_backward");
		Pan(panVector * (float)delta * PanKeySpeed * ZoomDistance);
    }

	private void ResetView()
	{
		Apply(_startState);
	}

	private CameraState Capture()
	{
		return new CameraState { 
			Distance = ZoomDistance,
			Yaw = Rotation.Y,
			Pitch = Arm.Rotation.X,
			PivotPosition = Position };
	}

	private void Apply(CameraState state)
	{
		SetZoom(state.Distance);
		SetPitch(state.Pitch);
		SetYaw(state.Yaw);
		SetPivotPosition(state.PivotPosition);
	}

	private void HandleMouseMotion(InputEventMouseMotion motion)
	{
		if (Input.IsActionPressed("camera_orbit"))
		{
			Vector2 sign = new(
				InvertYaw ? 1f : -1f,
				InvertPitch ? 1f : -1f
			);
			Orbit(motion.Relative * sign * OrbitSensitivity);
		}

		if (Input.IsActionPressed("camera_pan_drag"))
		{
			float sign = PanDragStyle == DragStyle.GrabWorld ? -1f : 1f;
			Pan(sign * motion.Relative * PanSensitivity * ZoomDistance);
		}
	}

	private void HandleMouseButton(InputEventMouseButton button)
	{
		float factor = InvertZoom ? 1f / ZoomFactor : ZoomFactor;	
		if (button.IsActionPressed("camera_zoom_in"))
		{
			SetZoom(ZoomDistance * factor);
		} else if (button.IsActionPressed("camera_zoom_out"))
		{
			SetZoom(ZoomDistance / factor);
		}
	}

	private void Orbit(Vector2 motionRad)
	{
		SetPitch(Arm.Rotation.X + motionRad.Y);
		SetYaw(Rotation.Y + motionRad.X);
	}

	private void Pan(Vector2 offset)
	{
		SetPivotPosition(Position + Basis.X * offset.X + Basis.Z * offset.Y);
	}

	private void SetPitch(float pitch)
	{
		Vector3 rotation = Arm.Rotation;
		rotation.X = Mathf.Clamp(
			pitch,
			Mathf.DegToRad(PitchMin), 
			Mathf.DegToRad(PitchMax));
		Arm.Rotation = rotation;
	}

	private void SetYaw(float yaw)
	{
		Vector3 rotation = Rotation;
		rotation.Y = yaw;
		Rotation = rotation;
	}
	
	private void SetZoom(float distance)
	{
		Vector3 zoom = Camera.Position;
		zoom.Z = Mathf.Clamp(distance, ZoomMin, ZoomMax);
		ZoomDistance = zoom.Z;
		Camera.Position = zoom;
	}

	private void SetPivotPosition(Vector3 position)
	{
		Position = position;
	}
}

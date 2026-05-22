using System;
using System.Collections.Generic;
using Godot;
using RtsGame.Presentation.GodotBridge;

internal sealed class RtsSelectionController
{
	private const float DragSelectionThresholdPixels = 6.0f;
	private readonly List<int> _selectedUnitIds = new List<int>();
	private int _selectedBuildingId;
	private bool _dragActive;
	private Vector2 _dragStart = Vector2.Zero;
	private Vector2 _dragCurrent = Vector2.Zero;

	public IReadOnlyList<int> SelectedUnitIds
	{
		get { return _selectedUnitIds; }
	}

	public int SelectedBuildingId
	{
		get { return _selectedBuildingId; }
	}

	public bool HasSelectedUnits
	{
		get { return _selectedUnitIds.Count > 0; }
	}

	public bool IsDragActive
	{
		get { return _dragActive; }
	}

	public Vector2 DragStart
	{
		get { return _dragStart; }
	}

	public Vector2 DragCurrent
	{
		get { return _dragCurrent; }
	}

	public void Reset()
	{
		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;
		_dragActive = false;
		_dragStart = Vector2.Zero;
		_dragCurrent = Vector2.Zero;
	}

	public void BeginDrag(Vector2 worldPosition)
	{
		_dragActive = true;
		_dragStart = worldPosition;
		_dragCurrent = worldPosition;
	}

	public void UpdateDrag(Vector2 worldPosition)
	{
		_dragCurrent = worldPosition;
	}

	public bool CompleteDragAndSelect(
		Vector2 releaseWorldPosition,
		GodotFrameDto frame,
		int localPlayerIndex,
		Func<float, long> screenToRaw,
		int clickedResourceId,
		int hoveredResourceId,
		Action<string> log)
	{
		if (!_dragActive)
		{
			return false;
		}

		_dragCurrent = releaseWorldPosition;
		Vector2 delta = _dragCurrent - _dragStart;
		bool isRectangleSelection = delta.LengthSquared() >= DragSelectionThresholdPixels * DragSelectionThresholdPixels;
		_dragActive = false;
		if (isRectangleSelection)
		{
			SelectUnitsInRectangle(frame, localPlayerIndex, screenToRaw, log);
		}
		else
		{
			SelectAt(frame, localPlayerIndex, _dragCurrent, screenToRaw, clickedResourceId, hoveredResourceId, log);
		}

		return true;
	}

	public void SelectAt(
		GodotFrameDto frame,
		int localPlayerIndex,
		Vector2 screenPosition,
		Func<float, long> screenToRaw,
		int clickedResourceId,
		int hoveredResourceId,
		Action<string> log)
	{
		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;

		GodotSelectionResult selection = GodotSelectionRouter.SelectAt(
			frame,
			localPlayerIndex,
			screenToRaw(screenPosition.X),
			screenToRaw(screenPosition.Y));

		if (selection.Kind == GodotSelectionKind.Unit)
		{
			_selectedUnitIds.Add(selection.EntityId);
			log("select unit=" + selection.EntityId);
			return;
		}

		if (selection.Kind == GodotSelectionKind.Building)
		{
			_selectedBuildingId = selection.EntityId;
			log("select building=" + selection.EntityId);
			return;
		}

		if (clickedResourceId != 0)
		{
			log("select resource=" + clickedResourceId + " hovered=" + hoveredResourceId);
			return;
		}

		log("selection cleared");
	}

	public int[] GetSelectedUnitIdsSorted()
	{
		int[] selected = _selectedUnitIds.ToArray();
		Array.Sort(selected);
		return selected;
	}

	public bool IsUnitSelected(int unitId)
	{
		return _selectedUnitIds.Contains(unitId);
	}

	private void SelectUnitsInRectangle(
		GodotFrameDto frame,
		int localPlayerIndex,
		Func<float, long> screenToRaw,
		Action<string> log)
	{
		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;

		int[] selected = GodotSelectionRouter.SelectUnitsInRectangle(
			frame,
			localPlayerIndex,
			screenToRaw(_dragStart.X),
			screenToRaw(_dragStart.Y),
			screenToRaw(_dragCurrent.X),
			screenToRaw(_dragCurrent.Y));

		for (int i = 0; i < selected.Length; i++)
		{
			_selectedUnitIds.Add(selected[i]);
		}

		if (selected.Length == 0)
		{
			log("box select none");
		}
		else
		{
			log("box select units=" + string.Join(",", selected));
		}
	}
}

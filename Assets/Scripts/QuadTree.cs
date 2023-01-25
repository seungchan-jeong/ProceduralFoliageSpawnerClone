using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TQuadTree<T> where T : class
{
	struct FNode
	{
		public Bounds bounds;
		public T elements;

		public FNode(T elements, Bounds bounds)
		{
			this.bounds = bounds;
			this.elements = elements;
		}
	}
	enum QuadNames
	{
		TopLeft = 0,
		TopRight = 1,
		BottomLeft = 2,
		BottomRight = 3
	}

	private List<FNode> _nodes;
	private TQuadTree<T>[] _subTrees;
	private Bounds _treeBox;
	private Vector2 _position; // x, z
	private float _minimumQuadSize;
	private bool _isInternal;
	
	public TQuadTree(Bounds InBox, float InMinimumQuadSize = 100.0f)
	{
		_nodes = new List<FNode>();
		_subTrees = new TQuadTree<T>[4];
		_treeBox = InBox;
		_position = new Vector2(InBox.center.x, InBox.center.z);
		_minimumQuadSize = InMinimumQuadSize;
		_isInternal = false;

		_subTrees[0] = _subTrees[1] = _subTrees[2] = _subTrees[3] = null;
	}

	public Bounds GetTreeBox()
	{
		return _treeBox;
	}

	public void Insert(T element, Bounds box)
	{
		if (!box.Intersects(_treeBox))
		{
			Debug.LogWarning($"Adding element {box.ToString()} this is outside the bounds of the quadtree root {box.ToString()}. Consider resizing.");
		}

		InsertElementRecursive(element, box);
	}

	private void InsertElementRecursive(T element, Bounds box)
	{
		TQuadTree<T>[] Quads = new TQuadTree<T>[4];
		int NumQuads = GetQuads(box, Quads);
		if (NumQuads == 0)
		{
			// It's possible that all elements in the leaf are bigger than the leaf or that more elements than NodeCapacity exist outside the top level quad
			// In either case, we can get into an endless spiral of splitting
			bool bCanSplitTree = _treeBox.size.sqrMagnitude > _minimumQuadSize * _minimumQuadSize;
			if (!bCanSplitTree || _nodes.Count < 4)
			{
				_nodes.Add(new FNode(element, box));

				if (!bCanSplitTree)
				{
					Debug.Log($"Minimum size {_minimumQuadSize} reached for quadtree at {_position.ToString()}. " +
					          $"Filling beyond capacity 4 to {_nodes.Count}");
				}
			}
			else
			{
				// This quad is at capacity, so split and try again
				Split();
				InsertElementRecursive(element, box);
			}
		}
		else if (NumQuads == 1)
		{
			Quads[0].InsertElementRecursive(element, box);
		}
		else
		{
			_nodes.Add(new FNode(element, box));
		}
	}

	private void Split()
	{
		Vector3 Extent = new Vector3(_treeBox.extents.x, 0.0f, _treeBox.extents.z);
		Vector3 XExtent = new Vector3(Extent.x, 0.0f, 0.0f);
		Vector3 YExtent = new Vector3(0.0f, 0.0f, Extent.z);

		/************************************************************************
		 *  ___________max
		 * |     |     | 
		 * |     |     |
		 * |-----c------
		 * |     |     |
		 * min___|_____|
		 *
		 * We create new quads by adding xExtent and yExtent
		 ************************************************************************/

		Vector3 C = new Vector3(_position.x, 0.0f, _position.y);
		Vector3 TM = C + YExtent;
		Vector3 ML = C - XExtent;
		Vector3 MR = C + XExtent;
		Vector3 BM = C - YExtent;
		Vector3 BL = new Vector3(_treeBox.min.x,0.0f, _treeBox.min.z);
		Vector3 TR = new Vector3(_treeBox.max.x, 0.0f, _treeBox.max.z);
		
		_subTrees[(int)QuadNames.TopLeft] = new TQuadTree<T>(new Bounds((ML + TM) * 0.5f, (TM - ML) * 0.5f), _minimumQuadSize);
		_subTrees[(int)QuadNames.TopRight] = new TQuadTree<T>(new Bounds((C + TR) * 0.5f, (TR - C) * 0.5f), _minimumQuadSize);
		_subTrees[(int)QuadNames.BottomLeft] = new TQuadTree<T>(new Bounds((BL + C) * 0.5f, (C - BL) * 0.5f), _minimumQuadSize);
		_subTrees[(int)QuadNames.BottomRight] = new TQuadTree<T>(new Bounds((BM + MR) * 0.5f, (MR - BM) * 0.5f), _minimumQuadSize);

		//mark as no longer a leaf
		_isInternal = true;

		// Place existing nodes and place them into the new subtrees that contain them
		// If a node overlaps multiple subtrees, we retain the reference to it here in this quad
		List<FNode> OverlappingNodes = new List<FNode>();
		foreach (FNode Node in _nodes)
		{
			TQuadTree<T>[] quads = new TQuadTree<T>[4];
			int NumQuads = GetQuads(Node.bounds, quads);

			if (NumQuads == 1)
			{
				quads[0]._nodes.Add(Node);
			}
			else
			{
				OverlappingNodes.Add(Node);
			}
		}

		// Hang onto the nodes that don't fit cleanly into a single subtree
		_nodes = OverlappingNodes;
	}

	public void GetElements(Bounds Box, List<T> ElementsOut)
	{
		TQuadTree<T>[] quads = new TQuadTree<T>[4];
		int numQuads = GetQuads(Box, quads);

		GetIntersectingElements(Box, ElementsOut);

		for (int i = 0; i < numQuads; i++)
		{
			quads[i].GetElements(Box, ElementsOut);
		}
	}

	private int GetQuads(Bounds box, TQuadTree<T>[] Quads)
	{
		int QuadCount = 0;
		if (_isInternal)
		{
			bool bNegX = box.min.x <= _position.x;
			bool bNegY = box.min.z <= _position.y;

			bool bPosX = box.max.x >= _position.x;
			bool bPosY = box.max.z >= _position.y;

			if (bNegX && bNegY)
			{
				Quads[QuadCount++] = _subTrees[(int)QuadNames.BottomLeft];
			}

			if (bPosX && bNegY)
			{
				Quads[QuadCount++] = _subTrees[(int)QuadNames.BottomRight];
			}

			if (bNegX && bPosY)
			{
				Quads[QuadCount++] = _subTrees[(int)QuadNames.TopLeft];
			}

			if (bPosX && bPosY)
			{
				Quads[QuadCount++] = _subTrees[(int)QuadNames.TopRight];
			}
		}

		return QuadCount;
	}

	private void GetIntersectingElements(Bounds box, List<T> elementsOut)
	{
		elementsOut.Capacity = elementsOut.Count + _nodes.Count;
		elementsOut.AddRange(from Node in _nodes where box.Intersects(Node.bounds) select Node.elements);
	}

	public bool Remove(T Element, Bounds Box)
	{
		bool bElementRemoved = false;
		TQuadTree<T>[] quads = new TQuadTree<T>[4];
		int numQuads = GetQuads(Box, quads);

		// Remove from nodes referenced by this quad
		bElementRemoved = RemoveNodeForElement(Element);

		// Try to remove from subtrees if necessary
		for (int i = 0; i < numQuads && !bElementRemoved; i++)
		{
			bElementRemoved = quads[i].Remove(Element, Box);
		}

		return bElementRemoved;
	}

	private bool RemoveNodeForElement(T element)
	{
		int ElementIdx = -1;
		for (int NodeIdx = 0, NumNodes = _nodes.Count; NodeIdx < NumNodes; ++NodeIdx)
		{
			if (_nodes[NodeIdx].elements == element) //TODO 이거 때문에 T를 where class 로 제한함. 꼭 필요한가? 
			{
				ElementIdx = NodeIdx;
				break;
			}
		}

		if (ElementIdx != -1)
		{
			_nodes.RemoveAt(ElementIdx);
			return true;
		}

		return false;
	}

	public void Duplicate(TQuadTree<T> OutDuplicate)
	{
		for (int TreeIdx = 0; TreeIdx < 4; ++TreeIdx)
		{
			TQuadTree<T> SubTree = _subTrees[TreeIdx];
			if (SubTree != null)
			{
				OutDuplicate._subTrees[TreeIdx] = new TQuadTree<T>(new Bounds(), _minimumQuadSize);
				SubTree.Duplicate(OutDuplicate._subTrees[TreeIdx]);	//duplicate sub trees
			}
		}

		OutDuplicate._nodes = _nodes;
		OutDuplicate._treeBox = _treeBox;
		OutDuplicate._position = _position;
		OutDuplicate._minimumQuadSize = _minimumQuadSize;
		OutDuplicate._isInternal = _isInternal;
	}

	public void Empty()
	{
		for (int TreeIdx = 0; TreeIdx < 4; ++TreeIdx)
		{
			_subTrees[TreeIdx] = null;
		}

		_nodes.Clear();
		_isInternal = false;
	}
	
}
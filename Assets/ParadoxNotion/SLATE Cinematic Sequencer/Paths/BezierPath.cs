/* Based on the free 'Bezier Curve Editor' by 'Arkham Interactive' */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Slate{

	///A Path defined out of BezierCurves
	[AddComponentMenu("SLATE/Path")]
	public class BezierPath : Path {
		
		public bool constantSpeedInterpolation = true;
		public int resolution = 30;
		public Color drawColor = Color.white;
		[SerializeField] [HideInInspector]
		private List<BezierPoint> _points = new List<BezierPoint>();

		private Vector3[] _sampledPathPoints;
		private float _length;
		private bool _closed;//not used right now

		public List<BezierPoint> points{
			get {return _points;}
		}

		public bool closed{
			get { return _closed; }
			set
			{
				if (_closed != value){
					_closed = value;
					SetDirty();
				}
			}
		}
		
		public BezierPoint this[int index]{
			get { return points[index]; }
		}
		
		public int pointCount{
			get { return points.Count; }
		}
		
		public override float length{
			get {return _length;}
		}

		public void SetDirty(){
			#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Path Change");
			#endif

			ComputeSampledPathPoints();
			ComputeLength();
		}

		void ComputeLength(){
			
			if (constantSpeedInterpolation){
				_length = GetLength(_sampledPathPoints);
				return;
			}
			
			_length = 0;
			for (int i = 0; i < points.Count - 1; i++){
				_length += ApproximateLength(points[i], points[i + 1], resolution);
			}
			
			if (closed){
				_length += ApproximateLength(points[points.Count - 1], points[0], resolution);
			}
		}

		void ComputeSampledPathPoints(){
			if (points.Count == 0){
				_sampledPathPoints = new Vector3[0];
				return;
			}

			var result = new List<Vector3>();
			for (int i = 0; i < points.Count - 1; i++){
				var current = points[i];
				var next = points[i+1];
				result.AddRange(GetSampledPathPoints(current, next, resolution));
			}

			_sampledPathPoints = result.ToArray();
		}

		void Reset(){
			AddPointAt(transform.position + new Vector3(-3,0,0));
			AddPointAt(transform.position + new Vector3(3,0,0));
			SetDirty();
		}

		void Awake(){ SetDirty(); }
		void OnValidate(){ SetDirty(); }
		
		void OnDrawGizmos () {
			Gizmos.color = drawColor;
			if (points.Count > 1){
				for (int i = 0; i < points.Count - 1; i++){
					DrawPath(points[i], points[i+1], resolution);
				}
				
				if (closed) DrawPath(points[points.Count - 1], points[0], resolution);
			}
			Gizmos.color = Color.white;
		}

		///Create a new BezierPath object
		public static BezierPath Create(Transform targetParent = null){
			var rootName = "[ PATHS ]";
			GameObject root = null;
			if (targetParent == null){
				root = GameObject.Find(rootName);
				if (root == null){
					root = new GameObject(rootName);
				}
			} else {
				var child = targetParent.Find(rootName);
				if (child != null){
					root = child.gameObject;
				} else {
					root = new GameObject(rootName);
				}
			}
			root.transform.SetParent(targetParent, false);

			var path = new GameObject("Path").AddComponent<BezierPath>();
			path.transform.SetParent(root.transform, false);
			path.transform.localPosition = Vector3.zero;
			path.transform.localRotation = Quaternion.identity;
			return path;
		}		

		///Add a new bezier point at index.
		public BezierPoint AddPointAt(Vector3 position, int index = -1){
			var newPoint = new BezierPoint(this, position);
			if (index == -1){
				points.Add(newPoint);
			} else {
				points.Insert(index, newPoint);
			}
			SetDirty();
			return newPoint;
		}
		
		///Remove a bezeri point.
		public void RemovePoint(BezierPoint point){
			points.Remove(point);
			SetDirty();
		}

		///Get a bezier point index.
		public int GetPointIndex(BezierPoint point){
			int result = -1;
			for(int i = 0; i < points.Count; i++){
				if(points[i] == point){
					result = i;
					break;
				}
			}
			
			return result;
		}

		public override Vector3 GetPointAt(float t){
			if (constantSpeedInterpolation){
				return GetUniformPointAt(t);
			}
			return GetApproximatePointAt(t);
		}

		///Get a Vector3 position along the path at normalized length t.
		public Vector3 GetApproximatePointAt(float t){
			if (t <= 0) return points[0].position;
			if (t >= 1) return points[points.Count - 1].position;
			
			float totalPercent = 0;
			float curvePercent = 0;
			
			BezierPoint p1 = null;
			BezierPoint p2 = null;
			
			for (int i = 0; i < points.Count - 1; i++){
				curvePercent = ApproximateLength(points[i], points[i + 1], 10) / length;
				if (totalPercent + curvePercent > t){
					p1 = points[i];
					p2 = points[i + 1];
					break;
				}
				
				else totalPercent += curvePercent;
			}
/*		
			if (closed && p1 == null){
				p1 = points[points.Count - 1];
				p2 = points[0];
			}
*/
			if (p1 == null){
				p1 = points[points.Count - 1];
				p2 = points[0];			
			}
			
			t -= totalPercent;
			
			return GetPoint(p1, p2, t / curvePercent);
		}
		
		
		///Get a uniform Vector3 position along the path at normalized length t.
		public Vector3 GetUniformPointAt(float t){
			if (t <= 0) return points[0].position;
			if (t >= 1) return points[points.Count - 1].position;
			return GetPoint(t, _sampledPathPoints);
		}


		public static Vector3[] GetSampledPathPoints(BezierPoint p1, BezierPoint p2, int resolution){
			var result = new List<Vector3>();
			int limit = resolution+1;
			float _res = resolution;

			for (int i = 1; i < limit; i++){
				var currentPoint = GetPoint(p1, p2, i/_res);
				result.Add(currentPoint);
			}

			return result.ToArray();
		}

	
		///Draw the path
		public static void DrawPath(BezierPoint p1, BezierPoint p2, int resolution){
			int limit = resolution+1;
			float _res = resolution;
			var lastPoint = p1.position;
			var currentPoint = Vector3.zero;
			
			for(int i = 1; i < limit; i++){
				currentPoint = GetPoint(p1, p2, i/_res);
				Gizmos.DrawLine(lastPoint, currentPoint);
				lastPoint = currentPoint;
			}		
		}	

		public static Vector3 GetPoint(BezierPoint p1, BezierPoint p2, float t){
			if (p1.handle2 != Vector3.zero){
				if (p2.handle1 != Vector3.zero) return GetCubicCurvePoint(p1.position, p1.globalHandle2, p2.globalHandle1, p2.position, t);
				else return GetQuadraticCurvePoint(p1.position, p1.globalHandle2, p2.position, t);
			} else {
				if (p2.handle1 != Vector3.zero) return GetQuadraticCurvePoint(p1.position, p2.globalHandle1, p2.position, t);
				else return GetLinearPoint(p1.position, p2.position, t);
			}	
		}

		public static float ApproximateLength(BezierPoint p1, BezierPoint p2, int resolution = 10){
			float _res = resolution;
			var total = 0f;
			var lastPosition = p1.position;
			Vector3 currentPosition;
			
			for(int i = 0; i < resolution + 1; i++)	{
				currentPosition = GetPoint(p1, p2, i / _res);
				total += (currentPosition - lastPosition).magnitude;
				lastPosition = currentPosition;
			}
			
			return total;
		}
		


		
/*
		public Vector3 GetPointAtDistance(float distance){
			if(closed) {
				if(distance < 0) while(distance < 0) { distance += length; }
				else if(distance > length) while(distance > length) { distance -= length; }
			
			} else {

				if(distance <= 0) return points[0].position;
				else if(distance >= length) return points[points.Count - 1].position;
			}
			
			float totalLength = 0;
			float curveLength = 0;
			
			BezierPoint firstPoint = null;
			BezierPoint secondPoint = null;
			
			for(int i = 0; i < points.Count - 1; i++){
				curveLength = ApproximateLength(points[i], points[i + 1], resolution);
				if(totalLength + curveLength >= distance){
					firstPoint = points[i];
					secondPoint = points[i+1];
					break;
				}
				else totalLength += curveLength;
			}
			
			if(firstPoint == null){
				firstPoint = points[points.Count - 1];
				secondPoint = points[0];
				curveLength = ApproximateLength(firstPoint, secondPoint, resolution);
			}
			
			distance -= totalLength;
			return GetPoint(distance / curveLength, firstPoint, secondPoint);
		}
*/

	}
}
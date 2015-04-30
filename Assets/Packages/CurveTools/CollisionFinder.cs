using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CurveTools {

	public class MeshTester {
		public static bool hitObject(Component[] objects, Ray rayWorld, out MeshFilter minMf, out TriangleTester.HitRes minHit) {
			List<BoundsDistance> mfHitBounds = new List<BoundsDistance>();
			foreach (var obj in objects) {
				var mf = obj.GetComponent<MeshFilter>();
				var renderer = obj.GetComponent<Renderer>();
				if (!obj.gameObject.activeInHierarchy || mf == null || renderer == null)
					continue;
				
				var bounds = renderer.bounds;
				float dist;
				if (!bounds.IntersectRay(rayWorld, out dist))
					continue;
				
				mfHitBounds.Add(new BoundsDistance(){ mf = mf, distance = dist });
				//Debug.Log("Candidate (Bounds) : " + mf.gameObject.name);
			}
			mfHitBounds.Sort((x, y) => (x.distance < y.distance ? -1 : 1));
			
			bool found = false;
			minMf = null;
			minHit = default(TriangleTester.HitRes);
			var minDist = Mathf.Infinity;
			foreach (BoundsDistance mfDists in mfHitBounds) {
				if (minDist < mfDists.distance)
					break;
				
				MeshFilter mf = mfDists.mf;
				Vector3 rayOrigin = mf.transform.InverseTransformPoint(rayWorld.origin);
				Vector3 rayDirection = mf.transform.worldToLocalMatrix * (Vector4) rayWorld.direction;
				Mesh m = mf.sharedMesh;
				TriangleTester.HitRes hit;
				if (!TriangleTester.hitTriangleAll(rayOrigin, rayDirection, m.vertices, m.triangles, out hit))
					continue;
				
				found = true;
				var dist = hit.t;
				if (dist < minDist) {
					minDist = dist;
					minHit = hit;
					minMf = mf;
				}
			}
			
			return found;
		}
		
		private struct BoundsDistance {
			public MeshFilter mf;
			public float distance;
		}
	}

	public class TriangleTester {
		public static int hitTriangle(Vector3 rayOrigin, Vector3 rayDirection,
				Vector3[] triVerts, int[] indices, int indicesOffset, out HitRes hit) {
			Vector3 edge1 = triVerts[indices[indicesOffset + 1]] - triVerts[indices[indicesOffset + 0]];
			Vector3 edge2 = triVerts[indices[indicesOffset + 2]] - triVerts[indices[indicesOffset + 0]];
			
			Vector3 p = Vector3.Cross(rayDirection, edge2);
			float det = Vector3.Dot(p, edge1);
			hit = default(HitRes);
			
			if (-Mathf.Epsilon < det && det < Mathf.Epsilon)
				return 0;
			float rDet = 1.0f / det;
			
			Vector3 vT = rayOrigin - triVerts[indices[indicesOffset + 0]];
			float u = Vector3.Dot(p, vT) * rDet;
			if (u < 0f || 1f < u)
				return 0;
			
			Vector3 q = Vector3.Cross(vT, edge1);
			float v = Vector3.Dot (q, rayDirection) * rDet;
			if (v < 0f || 1f < (u + v))
				return 0;
			
			float t = Vector3.Dot(q, edge2) * rDet;
			hit.u = u;
			hit.v = v;
			hit.t = t;
			
			return 1;
		}
		public static bool hitTriangleAll(Vector3 rayOrigin, Vector3 rayDirection, 
				Vector3[] vertices, int[] triangles, out HitRes minHit) {
			bool found = false;
			float minDist = Mathf.Infinity;
			minHit = default(HitRes);
			HitRes hit;		
			for (int triOffset = 0; triOffset < triangles.Length; triOffset+=3) {
				if (hitTriangle(rayOrigin, rayDirection, vertices, triangles, triOffset, out hit) == 0)
					continue;

				hit.i = triOffset;
				if (hit.t < minDist) {
					minDist = hit.t;
					minHit = hit;
					found = true;
				}
			}
			
			return found;
		}	
		public static float sqrDistanceFromPoint(Vector3 point, Vector3[] triVerts, int[] triIndices, int indicesOffset, float sqrMaxDist) {
			Vector3 v0 = triVerts[triIndices[indicesOffset + 0]];
			Vector3 axis1 = triVerts[triIndices[indicesOffset + 1]] - v0;
			Vector3 axis2 = triVerts[triIndices[indicesOffset + 2]] - v0;
			Vector3 normal = Vector3.Cross(axis1, axis2);
			Vector3 axis3 = normal.normalized;
			Vector3 b = point - v0;
			
			float det = Vector3.Dot(normal, axis3);
			float rDet = 1f / det;
			
			float t = Vector3.Dot(normal, b) * rDet;
			float sqrT = t * t;
			if (sqrT > sqrMaxDist)
				return Mathf.Infinity;
			
			Vector3 b3 = Vector3.Cross(b, axis3);
			float u = -Vector3.Dot(axis2, b3) * rDet;
			float v =  Vector3.Dot(axis1, b3) * rDet;
			
			u = u < 0f ? 0f : (u < 1f ? u : 1f);
			v = v < 0f ? 0f : (v < 1f ? v : 1f);
			Vector3 pContact = u * axis1 + v * axis2 + v0;
			
			return (point - pContact).sqrMagnitude;
		}
		
		public struct HitRes {
			public float t, u, v;
			public int i;
		}
	}
}
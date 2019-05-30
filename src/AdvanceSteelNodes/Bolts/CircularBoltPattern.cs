﻿using AdvanceSteel.Nodes.Plates;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.DesignScript.Runtime;
using DynGeometry = Autodesk.DesignScript.Geometry;
using SteelGeometry = Autodesk.AdvanceSteel.Geometry;
using SteelServices = Dynamo.Applications.AdvanceSteel.Services;
using Autodesk.AdvanceSteel.ConstructionTypes;
using System.Collections.Generic;
using Autodesk.AdvanceSteel.Geometry;
using System.Linq;
using System;

namespace AdvanceSteel.Nodes.Bolts
{
	/// <summary>
	/// Advance Steel plate
	/// </summary>
	[DynamoServices.RegisterForTrace]
	public class CircularBoltPattern : GraphicObject
	{
		internal CircularBoltPattern(SteelGeometry.Point3d astPointRef, double radius, IEnumerable<string> handlesToConnect, int nBolts, SteelGeometry.Vector3d vx, SteelGeometry.Vector3d vy)
		{
			lock (access_obj)
			{
				using (var ctx = new SteelServices.DocContext())
				{
					Autodesk.AdvanceSteel.Modelling.CircleScrewBoltPattern bolts = null;

					string handle = SteelServices.ElementBinder.GetHandleFromTrace();
					if (string.IsNullOrEmpty(handle) || Utils.GetObject(handle) == null)
					{
						bolts = new Autodesk.AdvanceSteel.Modelling.CircleScrewBoltPattern(astPointRef, vx, vy);
						bolts.NumberOfScrews = nBolts;
						bolts.Radius = Utils.ToInternalUnits(radius, true);

						bolts.WriteToDb();

						HashSet<FilerObject> myHashSet = new HashSet<FilerObject>();

						foreach (var objHandle in handlesToConnect)
						{
							FilerObject obj = Utils.GetObject(objHandle);
							if (obj != null && (obj.IsKindOf(FilerObject.eObjectType.kBeam) ||
																	obj.IsKindOf(FilerObject.eObjectType.kBentBeam) ||
																	obj.IsKindOf(FilerObject.eObjectType.kPlate)))
							{
								myHashSet.Add(obj);
							}
							else
							{
								throw new System.Exception("Object is empty");
							}
						}
						bolts.Connect(myHashSet, AtomicElement.eAssemblyLocation.kOnSite);
					}
					else
					{
						bolts = Utils.GetObject(handle) as Autodesk.AdvanceSteel.Modelling.CircleScrewBoltPattern;

						if (bolts != null && bolts.IsKindOf(FilerObject.eObjectType.kCircleScrewBoltPattern))
						{
							bolts.RefPoint = astPointRef;
							bolts.NumberOfScrews = nBolts;
							bolts.Radius = Utils.ToInternalUnits(radius, true);
							bolts.XDirection = vx;
							bolts.YDirection = vy;

							HashSet<FilerObject> myHashSet = new HashSet<FilerObject>();

							foreach (var objHandle in handlesToConnect)
							{
								myHashSet.Add(Utils.GetObject(objHandle));
							}

							bolts.Connect(myHashSet, AtomicElement.eAssemblyLocation.kOnSite);
						}
						else
							throw new System.Exception("Not a circularPattern");
					}

					Handle = bolts.Handle;
					SteelServices.ElementBinder.CleanupAndSetElementForTrace(bolts);
				}
			}
		}

		/// <summary>
		/// Create an Advance Steel Circular Pattern By Circle
		/// </summary>
		/// <param name="circle">Input circle</param>
		/// <param name="objectsToConnect">Input objects</param>
		/// <param name="nBolts">Input no. of bolts</param>
		/// <returns></returns>
		public static CircularBoltPattern ByCircle(Autodesk.DesignScript.Geometry.Circle circle, IEnumerable<SteelDbObject> objectsToConnect, int nBolts)
		{
			var norm = Utils.ToAstVector3d(circle.Normal, true);

			var vx = norm.GetPerpVector();
			var vy = norm.CrossProduct(vx);

			// To_Erase:
			//v1
			/*
			IEnumerable<SteelDbObject> acceptedObjects = objectsToConnect.Where(obj =>
				obj is AdvanceSteel.Nodes.Beams.BentBeam ||
				obj is AdvanceSteel.Nodes.Beams.StraightBeam ||
				obj is AdvanceSteel.Nodes.Plates.Plate);

			if (acceptedObjects.Count() != objectsToConnect.Count())
				throw new Exception("only beams and plates...");

			IEnumerable<string> handles = acceptedObjects.Select(obj => obj.Handle);
			*/
			//~v1

			//v2
			List<string> handlesList = new List<string>();
			foreach (var obj in objectsToConnect)
			{
				if (obj is AdvanceSteel.Nodes.Beams.BentBeam ||
				obj is AdvanceSteel.Nodes.Beams.StraightBeam ||
				obj is AdvanceSteel.Nodes.Plates.Plate)
				{
					handlesList.Add(obj.Handle);
				}
				else
				{
					throw new Exception("only beams and plates...");
				}
			}
			IEnumerable<string> handles = handlesList;
			//handles = handlesList;
			//~v2

			return new CircularBoltPattern(Utils.ToAstPoint(circle.CenterPoint, true), circle.Radius, handles, nBolts, vx, vy);
		}

		/// <summary>
		/// Create an Advance Steel Circular Pattern By Point
		/// </summary>
		/// <param name="point">Input circle center point</param>
		/// <param name="radius">Input objects</param>
		/// <param name="normal">Input norm </param>
		/// <param name="objectsToConnect">Input objects</param>
		/// <param name="nBolts">Input no. of bolts</param>
		/// <returns></returns>
		public static CircularBoltPattern ByCenterPointRadiusNormal(DynGeometry.Point point, double radius, IEnumerable<SteelDbObject> objectsToConnect, int nBolts, DynGeometry.Vector normal)
		{
			SteelGeometry.Point3d astPointRef = Utils.ToAstPoint(point, true);

			var circle = DynGeometry.Circle.ByCenterPointRadius(point, radius);
			var norm = Utils.ToAstVector3d(normal, true);

			var vx = norm.GetPerpVector();
			var vy = norm.CrossProduct(vx);
			IEnumerable<string> handles = objectsToConnect.Select(obj => obj.Handle);
			return new CircularBoltPattern(Utils.ToAstPoint(circle.CenterPoint, true), circle.Radius, handles, nBolts, vx, vy);
		}

		[IsVisibleInDynamoLibrary(false)]
		public override Autodesk.DesignScript.Geometry.Curve GetDynCurve()
		{
			lock (access_obj)
			{
				using (var ctx = new SteelServices.DocContext())
				{
					var boltPattern = Utils.GetObject(Handle) as Autodesk.AdvanceSteel.Modelling.CircleScrewBoltPattern;

					using (var point = Utils.ToDynPoint(boltPattern.CenterPoint, true))
					using (var norm = Utils.ToDynVector(boltPattern.Normal, true))
					{
						return Autodesk.DesignScript.Geometry.Circle.ByCenterPointRadiusNormal(point, (boltPattern.Radius / 25.4), norm);
					}
				}
			}
		}
	}
}
﻿using System;
using AdvanceSteel.Nodes.Plates;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.DesignScript.Runtime;
using DynGeometry = Autodesk.DesignScript.Geometry;
using SteelGeometry = Autodesk.AdvanceSteel.Geometry;
using SteelServices = Dynamo.Applications.AdvanceSteel.Services;
using Autodesk.AdvanceSteel.ConstructionTypes;
using System.Collections.Generic;
using Autodesk.AdvanceSteel.Geometry;
using System.Linq;

namespace AdvanceSteel.Nodes.ConnectionObjects
{
	/// <summary>
	/// Advance Steel Rectangular Shear Stud Pattern
	/// </summary>
	[DynamoServices.RegisterForTrace]
	public class RectangularShearStudsPattern : GraphicObject
	{
		
		internal void UpdateShearStudPattern(Autodesk.AdvanceSteel.Modelling.Connector toUpdate, int nx, int ny, double dx, double dy, double length, double studDiameter)
		{
			toUpdate.Arranger.Nx = nx;
			toUpdate.Arranger.Ny = ny;
			toUpdate.Arranger.Dx = dx;
			toUpdate.Arranger.Dy = dy;
			toUpdate.Diameter = studDiameter;
			toUpdate.Length = length;
		}

		internal RectangularShearStudsPattern(SteelGeometry.Point3d astPoint1, SteelGeometry.Point3d astPoint2, string handleToConnect, SteelGeometry.Vector3d vx, SteelGeometry.Vector3d vy,
																					SteelGeometry.Matrix3d coordSyst, int nx, int ny, double studLen, double studDiameter)
		{
			lock (access_obj)
			{
				var dx = ObjectsConnection.GetRectangleLength(astPoint1, astPoint2, vx) / (nx - 1);
				var dy = ObjectsConnection.GetRectangleHeight(astPoint1, astPoint2, vx) / (ny - 1);

				using (var ctx = new SteelServices.DocContext())
				{
					Autodesk.AdvanceSteel.Modelling.Connector shearStuds = null;
					string handle = SteelServices.ElementBinder.GetHandleFromTrace();
					if (string.IsNullOrEmpty(handle) || Utils.GetObject(handle) == null)
					{
						shearStuds = new Autodesk.AdvanceSteel.Modelling.Connector
						{
							Arranger = new Autodesk.AdvanceSteel.Arrangement.RectangularArranger(Matrix2d.kIdentity, dx, dy, nx, ny)
						};
						shearStuds.WriteToDb();
					}
					else
					{
						shearStuds = Utils.GetObject(handle) as Autodesk.AdvanceSteel.Modelling.Connector;
						if (shearStuds == null || !shearStuds.IsKindOf(FilerObject.eObjectType.kConnector))
							throw new System.Exception("Not a shear stud pattern");
					}

					UpdateShearStudPattern(shearStuds, nx, ny, dx, dy, studLen, studDiameter);
					FilerObject obj = Utils.GetObject(handleToConnect);
					shearStuds.Connect(obj, coordSyst);

					Handle = shearStuds.Handle;
					SteelServices.ElementBinder.CleanupAndSetElementForTrace(shearStuds);
				}
			}
		}

		/// <summary>
		/// Create an Advance Steel Rectangular Shear Stud Pattern By Rectangle
		/// </summary>
		/// <param name="rectangle"> Input rectangle </param>
		/// <param name="objectToConnect"> Input objects </param>
		/// <param name="nx"> Input nx </param>
		/// <param name="ny"> Input ny </param>
		/// <param name="length"> Input length </param>
		/// <param name="diameter"> Input diameter </param>
		public static RectangularShearStudsPattern ByRectangle(Autodesk.DesignScript.Geometry.Rectangle rectangle, SteelDbObject objectToConnect,
																											     int nx, int ny, double length, double diameter)
		{
			var dynCorners = rectangle.Corners();
			var astCorners = Utils.ToAstPoints(dynCorners, true);
			var vx = astCorners[1] - astCorners[0];
			var vy = astCorners[3] - astCorners[0];
			var vz = Utils.ToAstVector3d(rectangle.Normal, true);

			vx = vx.Normalize();
			vy = vy.Normalize();
			vz = vz.Normalize();

			List<SteelDbObject> tempList = new List<SteelDbObject>() { objectToConnect };
			List<string> handlesList = ObjectsConnection.GetSteelDbObjectsToConnect(tempList);

			var rectangleCenter = astCorners[0] + (astCorners[2] - astCorners[0]) * 0.5;
			Matrix3d matrix3D = new Matrix3d();
			matrix3D.SetCoordSystem(rectangleCenter, vx, vy, vz);
			return new RectangularShearStudsPattern(astCorners[0], astCorners[2], handlesList[0], vx, vy, matrix3D, nx, ny, Utils.ToInternalUnits(length, true), Utils.ToInternalUnits(diameter, true));
		}

		[IsVisibleInDynamoLibrary(false)]
		public override Autodesk.DesignScript.Geometry.Curve GetDynCurve()
		{
			lock (access_obj)
			{
				using (var ctx = new SteelServices.DocContext())
				{
					var shearStud = Utils.GetObject(Handle) as Autodesk.AdvanceSteel.Modelling.Connector;
					if (shearStud == null)
					{
						throw new Exception("Null shear stud pattern");
					}

					var coordSystem = shearStud.CS;
					// Vx and Vy direction
					var tempVx = new Vector3d(coordSystem.Values[0][0], coordSystem.Values[1][0], coordSystem.Values[2][0]);
					var tempVy = new Vector3d(coordSystem.Values[0][1], coordSystem.Values[1][1], coordSystem.Values[2][1]);
					
					var tempXlen = shearStud.Arranger.Dx * (shearStud.Arranger.Nx - 1) / 2.0;
					var tempYlen = shearStud.Arranger.Dy * (shearStud.Arranger.Ny - 1) / 2.0;

					var temp1 = tempVx * tempXlen;
					var temp2 = tempVy * tempYlen;

					var pt1 = new SteelGeometry.Point3d(shearStud.CenterPoint);
					pt1.Add(temp1 + temp2);

					var pt2 = new SteelGeometry.Point3d(shearStud.CenterPoint);
					pt2.Add(temp1 - temp2);

					var pt3 = new SteelGeometry.Point3d(shearStud.CenterPoint);
					pt3.Add(-temp1 - temp2);

					var pt4 = new SteelGeometry.Point3d(shearStud.CenterPoint);
					pt4.Add(-temp1 + temp2);

					{
						List<DynGeometry.Point> polyPoints = new List<DynGeometry.Point>
						{
							Utils.ToDynPoint(pt1, true),
							Utils.ToDynPoint(pt2, true),
							Utils.ToDynPoint(pt3, true),
							Utils.ToDynPoint(pt4, true)
						};

						return Autodesk.DesignScript.Geometry.Polygon.ByPoints(polyPoints);
					}
				}
			}
		}

	}
}

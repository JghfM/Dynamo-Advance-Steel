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

namespace AdvanceSteel.Nodes.ConnectionObjects
{
	[IsVisibleInDynamoLibrary(true)]
	public class ObjectsConnection
	{
		public static HashSet<FilerObject> GetSteelObjectsToConnect(IEnumerable<string> handlesToConnect)
		{
			var ret = new HashSet<FilerObject>();
			foreach (var objHandle in handlesToConnect)
			{
				FilerObject obj = Utils.GetObject(objHandle);
				if (obj != null && (obj.IsKindOf(FilerObject.eObjectType.kBeam) ||
														obj.IsKindOf(FilerObject.eObjectType.kBentBeam) ||
														obj.IsKindOf(FilerObject.eObjectType.kPlate)))
				{
					ret.Add(obj);
				}
				else
				{
					throw new System.Exception("Object is empty");
				}
			}
			return ret;
		}
		
		public static HashSet<FilerObject> GetFilerObjects(IEnumerable<string> handles)
		{
			var ret = new HashSet<FilerObject>();
			foreach (var objHandle in handles)
			{
				ret.Add(Utils.GetObject(objHandle));
			}

			return ret;
		}
		
		public static List<string> GetSteelDbObjectsToConnect(IEnumerable<SteelDbObject> objectsToConnect)
		{
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
					throw new Exception("Only beams and plates can be connected");
				}
			}
			return handlesList;
		}
		
		/// <summary>
		/// Set Assembly Location 
		/// </summary>
		/// <param name="location">Input assembly location</param>
		/// <param name="aux">Input aux </param>
		/// <returns></returns>
		public static void SetAssemblyLocation(Autodesk.AdvanceSteel.Modelling.ScrewBoltPattern aux, AssemblyLocation location)
		{
			var temp = Utils.GetObject(aux.Handle) as Autodesk.AdvanceSteel.Modelling.ScrewBoltPattern;
			if (temp.IsKindOf(FilerObject.eObjectType.kCircleScrewBoltPattern) || temp.IsKindOf(FilerObject.eObjectType.kFinitRectScrewBoltPattern))
			{
				if (location == AssemblyLocation.kWrong)
					temp.AssemblyLocation = AtomicElement.eAssemblyLocation.kWrong;
				else if (location == AssemblyLocation.kUnknown)
					temp.AssemblyLocation = AtomicElement.eAssemblyLocation.kUnknown;
				else if (location == AssemblyLocation.kSiteDrill)
					temp.AssemblyLocation = AtomicElement.eAssemblyLocation.kSiteDrill;
				else if (location == AssemblyLocation.kOnSite)
					temp.AssemblyLocation = AtomicElement.eAssemblyLocation.kOnSite;
				else
					temp.AssemblyLocation = AtomicElement.eAssemblyLocation.kInShop;
			}
		}
		/// <summary>
		/// Get Assembly Location From Dynamo to Advance Steel
		/// </summary>
		public static AtomicElement.eAssemblyLocation GetSteelAssemblyLocation(AssemblyLocation assemblyLocation)
		{
			if (assemblyLocation == AssemblyLocation.kWrong)
				return AtomicElement.eAssemblyLocation.kWrong;
			else if (assemblyLocation == AssemblyLocation.kUnknown)
				return AtomicElement.eAssemblyLocation.kUnknown;
			else if (assemblyLocation == AssemblyLocation.kOnSite)
				return AtomicElement.eAssemblyLocation.kOnSite;
			else if (assemblyLocation == AssemblyLocation.kSiteDrill)
				return AtomicElement.eAssemblyLocation.kSiteDrill;
			else
				return AtomicElement.eAssemblyLocation.kInShop;
		}
		/// <summary>
		/// Get Assembly Location From Advance Steel to Dynamo
		/// </summary>
		public static AssemblyLocation GetDynAssemblyLocation(AtomicElement.eAssemblyLocation assemblyLocation)
		{
			if (assemblyLocation == AtomicElement.eAssemblyLocation.kWrong)
				return AssemblyLocation.kWrong;
			else if (assemblyLocation == AtomicElement.eAssemblyLocation.kUnknown)
				return AssemblyLocation.kUnknown;
			else if (assemblyLocation == AtomicElement.eAssemblyLocation.kOnSite)
				return AssemblyLocation.kOnSite;
			else if (assemblyLocation == AtomicElement.eAssemblyLocation.kSiteDrill)
				return AssemblyLocation.kSiteDrill;
			else
				return AssemblyLocation.kInShop;
		}
		/// <summary>
		/// Get Orientation Type From Dynamo to Advance Steel
		/// </summary>
		public static Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType GetSteelOrientationType(AnchorOrientationTypes.OrientationType orientationType)
		{
			if (orientationType == AnchorOrientationTypes.OrientationType.kNormalOrientation)
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kNormalOrientation;
			else if (orientationType == AnchorOrientationTypes.OrientationType.kDiagonalInside)
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kDiagonalInside;
			else if (orientationType == AnchorOrientationTypes.OrientationType.kDiagonalOutside)
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kDiagonalOutside;
			else if (orientationType == AnchorOrientationTypes.OrientationType.kAllOutside)
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kAllOutside;
			else if (orientationType == AnchorOrientationTypes.OrientationType.kAllInside)
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kAllInside;
			else if (orientationType == AnchorOrientationTypes.OrientationType.kInsideRotated)
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kInsideRotated;
			else
				return Autodesk.AdvanceSteel.Modelling.AnchorPattern.eOrientationType.kOutsideRotated;

		}

		/// <summary>
		/// Set anchors Orientation for Rectangular Anchor Pattern
		/// </summary>
		public static void SetRectangularAnchorPatternOrientation(AdvanceSteel.Nodes.ConnectionObjects.RectangularAnchorPattern element, AnchorOrientationTypes.OrientationType orientationType)
		{
			//lock the document and start transaction
			using (var ctx = new SteelServices.DocContext())
			{
				string handle = element.Handle;
				var anchors = Utils.GetObject(handle) as Autodesk.AdvanceSteel.Modelling.AnchorPattern;

				if (handle != null)
				{
					anchors.OrientationType = GetSteelOrientationType(orientationType);
				
				}
				else
					throw new System.Exception("Failed to set anchor type");
			}
		}

		/// <summary>
		/// Set anchors Orientation for Circular Anchor Pattern
		/// </summary>
		public static void SetRectangularAnchorPatternOrientation(AdvanceSteel.Nodes.ConnectionObjects.CircularAnchorPattern element, AnchorOrientationTypes.OrientationType orientationType)
		{
			//lock the document and start transaction
			using (var ctx = new SteelServices.DocContext())
			{
				string handle = element.Handle;
				var anchors = Utils.GetObject(handle) as Autodesk.AdvanceSteel.Modelling.AnchorPattern;

				if (handle != null)
				{
					anchors.OrientationType = GetSteelOrientationType(orientationType);

				}
				else
					throw new System.Exception("Failed to set anchor type");
			}
		}

	}
}



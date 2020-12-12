//using Assets.Scripts.Craft.Parts;
//using Assets.Scripts.Design;
//using ModApi.Craft.Parts;
//using ModApi.Design;
//using System.Collections.Generic;
//using System.Xml.Linq;
//using UnityEngine;
//using ObjParser;
//using System.Linq;
//using static System.Math;
//using Game = Assets.Scripts.Game;
//using ObjParser.Types;
//using SimpleFileBrowser;

//public class BuildMesh
//{
//    public static IDesigner _designer = Game.Instance.Designer;
//    public static DesignerScript _designerScript => (DesignerScript)Game.Instance.Designer;
//    public static DesignerPartList _designerParts = Assets.Scripts.Game.Instance.CachedDesignerParts;

//    public static List<IPartScript> facePartList = new List<IPartScript>();

//    public static void BuildMeshFromFile()
//    {
//        //unsubscribes ReselectRoot during mesh building
//        Game.Instance.Designer.SelectedPartChanged -= ReselectRoot;

//        DesignerPart _Fuselage1 = GetDesignerPart("Fuselage1"); //gets "Fuselage1" XML
//        XElement fuselageXml = _Fuselage1.AssemblyElement;
//        //var defaultFuselageXml = fuselageXml; //used to set Fuselage1 back to default after mesh is built (but doesn't work, because it always mirrors fuselageXml somehow)
//        XElement fuselageElement = fuselageXml.Descendants().First(x => x.Name == "Fuselage");

//        //mesh import settings
//        double meshShellWidth = .005;
//        double meshScale = 1;

//        //Obj mesh = ObjFileSelect();
//        Obj mesh = new Obj();
//        string hardFilePath = @"D:\SR2_mod_files\3D_Models\BoxHouseTestTris.obj";
//        mesh.LoadObj(hardFilePath);

//        if (mesh == null)
//        {
//            Debug.Log("No file selected.");
//            return;
//        }
//        //get file name and makes part names
//        string fileName = hardFilePath.Substring(hardFilePath.LastIndexOf('\\') + 1, hardFilePath.LastIndexOf('.') - (hardFilePath.LastIndexOf('\\') + 1));
//        Debug.Log("File name: " + fileName);
//        string centerPartName = fileName + "CenterPart";
//        string facePartName = fileName + "FacePart";

//        var faces = mesh.FaceList;
//        var verts = mesh.VertexList;

//        //scaling mesh
//        if (meshScale != 1)
//        {
//            List<Vertex> newVerts = new List<Vertex>();
//            foreach (var vert in verts)
//            {
//                vert.X *= meshScale;
//                vert.Y *= meshScale;
//                vert.Z *= meshScale;
//                newVerts.Add(vert);
//            }
//            verts = newVerts;
//        }

//        fuselageElement.SafeAddAttribute("bottomScale", "0.1,0.1");
//        fuselageElement.SafeAddAttribute("topScale", "0.1,0.1");
//        fuselageElement.SafeAddAttribute("offset", "0,0.1,0");
//        //disables resize handles for root part
//        fuselageElement.SafeAddAttribute("toolResizeBottom", "false");
//        fuselageElement.SafeAddAttribute("toolResizeHeight", "false");
//        fuselageElement.SafeAddAttribute("toolResizeRadius", "false");
//        fuselageElement.SafeAddAttribute("toolResizeTop", "false");

//        _designerScript.AddPart(_Fuselage1, new Vector2(0, 0));
//        PartScript _centerPart = (PartScript)_designerScript.CraftScript.Data.Assembly.Parts.Last().PartScript;
//        _centerPart.name = centerPartName;
//        _centerPart.GameObject.transform.position = new Vector3(0, 0, 0);

//        var centerAttachPointArray = _centerPart.Data.AttachPoints.ToArray();
//        AttachPoint centerAttachPoint = centerAttachPointArray[0];

//        Debug.Log("Building mesh with settings\n" +
//            "Shell Width: " + meshShellWidth +
//            "Scale: " + meshScale);

//        foreach (var face in faces)
//        {
//            //get face vertex coords
//            var faceVerts = face.VertexIndexList;
//            var vert0 = new Vector3((float)verts[faceVerts[0] - 1].X, (float)verts[faceVerts[0] - 1].Y, (float)verts[faceVerts[0] - 1].Z * -1); // - 1 because values in VertexIndexList start from 1...
//            var vert1 = new Vector3((float)verts[faceVerts[1] - 1].X, (float)verts[faceVerts[1] - 1].Y, (float)verts[faceVerts[1] - 1].Z * -1);
//            var vert2 = new Vector3((float)verts[faceVerts[2] - 1].X, (float)verts[faceVerts[2] - 1].Y, (float)verts[faceVerts[2] - 1].Z * -1);

//            //get values for shaping part to face
//            var basePos = (vert0 + vert1) / 2;
//            var partPos = (basePos + vert2) / 2;
//            var offset = basePos - vert2;
//            var baseWidth = (vert0 - vert1).magnitude;

//            //get orientation and axes
//            var vForward = (vert1 - vert0).normalized;
//            var vRight = Vector3.Cross(vForward, offset).normalized;
//            var vUp = Vector3.Cross(vForward, vRight).normalized;
//            var xAxis = new Vector3(-1, 0, 0);
//            var yAxis = new Vector3(0, -1, 0);
//            var zAxis = new Vector3(0, 0, -1);

//            //first rotation of offset vector
//            var firstRotAxis = Vector3.Cross(vRight, zAxis).normalized;
//            var firstRotAngle = Acos(Vector3.Dot(vRight, zAxis));
//            var offsetFirstRot = offset.VectorRotate(firstRotAxis, firstRotAngle);
//            var vUpRotated = vUp.VectorRotate(firstRotAxis, firstRotAngle);
//            //Debug.Log("First rotation axis: " + firstRotAxis);

//            //second rotation of offset vector
//            var finalRotAxis = Vector3.Cross(vUpRotated, yAxis).normalized;
//            var finalRotAngle = Acos(Vector3.Dot(vUpRotated, yAxis));
//            var offsetFinalRot = offsetFirstRot.VectorRotate(Vector3.Cross(vUpRotated, yAxis).normalized, Acos(Vector3.Dot(vUpRotated, yAxis)));
//            //Debug.Log("Final rotation: " + finalRotAngle * (180 / PI));

//            if (firstRotAxis.magnitude == 0 && firstRotAngle > 1.5) //When vRight and zAxis are opposite, cross prod == 0 and no rotation happens. Desired behaviour is 180deg rotation, aka negative offset.x
//            {
//                offsetFinalRot.x *= -1;
//            }
//            offsetFinalRot *= (float).5; //because game values are half of the vector
//            baseWidth *= (float).5;

//            //adding attributes to the part XML
//            fuselageElement.SafeAddAttribute("bottomScale", baseWidth + "," + meshShellWidth);
//            fuselageElement.SafeAddAttribute("topScale", "0," + meshShellWidth);
//            fuselageElement.SafeAddAttribute("offset", offsetFinalRot.x.ToString() + ',' + offsetFinalRot.y.ToString() + ',' + offsetFinalRot.z.ToString());
//            fuselageElement.SafeAddAttribute("cornerRadiuses", "0,0,0,0,0,0,0,0");
//            fuselageElement.SafeAddAttribute("autoResize", "false");

//            XElement partElement = fuselageXml.GetXElement("Part");
//            partElement.SafeAddAttribute("texture", "Default");
//            //Debug.Log("Final XML:\n" + fuselageXml);

//            //XML finished, adding and transforming GameObject now
//            _designerScript.AddPart(_Fuselage1, new Vector2(0, 0));
//            PartScript _referencePart = (PartScript)_designerScript.CraftScript.Data.Assembly.Parts.Last().PartScript;
//            _referencePart.name = facePartName;

//            //sets partPos and partRot for final transforms (also parent)
//            Quaternion partRot = new Quaternion();
//            partRot.SetLookRotation(-1 * vRight, vUp);
//            partPos -= vRight * (float)(.99 * meshShellWidth);
//            Vector3 eulerAngles = partRot.eulerAngles;
//            Debug.Log(eulerAngles);

//            _referencePart.GameObject.transform.position = partPos;
//            _referencePart.GameObject.transform.rotation = partRot;
//            //_referencePart.GameObject.transform.parent = _designer.CraftScript.RootPart.GameObject.transform.parent; //maybe this works?

//            //attaching part to root part
//            var refAttachPointArray = _referencePart.Data.AttachPoints.ToArray();
//            AttachPoint refAttachPoint = refAttachPointArray[1];

//            _referencePart.Data.PartConnections.Add(new PartConnection(_centerPart.Data, _referencePart.Data));
//            var refPartConnection = _referencePart.Data.PartConnections.Last();
//            refPartConnection.AddAttachment(centerAttachPoint, refAttachPoint);

//            //adds part to faceParts list, to be used in ReselectRoot()
//            if (!facePartList.Contains(_referencePart))
//            {
//                facePartList.Add(_referencePart);
//            }
//        }
//        //resetting Fuselage1 back to defaults manually
//        fuselageElement.SafeAddAttribute("bottomScale", "0.8,0.8");
//        fuselageElement.SafeAddAttribute("topScale", "0.8,0.8");
//        fuselageElement.SafeAddAttribute("offset", "0,2.5,0");
//        fuselageElement.SafeAddAttribute("cornerRadiuses", "1,1,1,1,1,1,1,1");
//        fuselageElement.SafeAddAttribute("autoResize", "true");
//        fuselageElement.SafeAddAttribute("toolResizeBottom", "true");
//        fuselageElement.SafeAddAttribute("toolResizeHeight", "true");
//        fuselageElement.SafeAddAttribute("toolResizeRadius", "true");
//        fuselageElement.SafeAddAttribute("toolResizeTop", "true");
//        XElement defaultPartElement = fuselageXml.GetXElement("Part");
//        defaultPartElement.SafeAddAttribute("texture", "Fuselage-1");

//        if (_designer.SelectedPart != null) {
//            _designer.DeselectPart();
//            }
//        _designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
//        _designer.ShowMessage("Mesh imported successfully");

//        var testQuat = Quaternion.LookRotation(new Vector3(1, 0, 0), new Vector3(0, 1, 0));
//        Debug.Log(testQuat.eulerAngles);

//        //subscribe ReselectRoot to PartSelected event
//        Game.Instance.Designer.SelectedPartChanged += ReselectRoot;
//    }

//    public static DesignerPart GetDesignerPart(string partId)
//    {
//        foreach (DesignerPart part in _designerParts.Parts)
//        {
//            if (part.PartTypes.First().Id == partId)
//            {
//                return part;
//            }
//        }
//        return null;
//    }

//    public static Obj ObjFileSelect()
//    {
//        string filePath = "";

//        //FileBrowser.SetFilters(false, new FileBrowser.Filter("Mesh", ".obj"));
//        //FileBrowser.SetDefaultFilter(".obj");
//        FileBrowser.ShowLoadDialog(path => { filePath = path[0]; }, null, false, false, null, "Select .obj file", "Select");

//        //this is the example useage
//        //FileBrowser.ShowLoadDialog((paths) => { Debug.Log("Selected: " + paths[0]); }, () => { Debug.Log("Canceled"); }, true, false, null, "Select Folder", "Select");

//        if (FileBrowser.Success)
//        {
//            return ValidateObj(filePath);
//        }
//        else
//        {
//            return null;
//        }
//    }

//    public static Obj ValidateObj(string filePath)
//    {
//        int invalidFaces = 0;

//        Obj obj = new Obj();
//        obj.LoadObj(filePath);

//        foreach (Face face in obj.FaceList)
//        {
//            if (face.VertexIndexList.Length > 3)
//            {
//                invalidFaces++;
//            }
//        }
//        if (invalidFaces != 0)
//        {
//            Debug.LogError("Error: Mesh has " + invalidFaces + " face" + ((invalidFaces == 1) ? "" : "s") + " with more than 3 sides. All faces must be triangulated.");
//            return null;
//        }
//        else
//        {
//            return obj;
//        }
//    }

//    public static void ReselectRoot(IPartScript oldPart, IPartScript newPart) //need to rethink this method of finding a part. PartScript.name gets replaced when scenes reload, so maybe store part id of all FaceParts
//    {
//        //if (newPart != null)
//        //{
//        //    string selectedPartName = newPart.GameObject.GetComponent<PartScript>().name;
//        //    string meshName = "";
//        //    if (selectedPartName.IndexOf("CenterPart") > 0)
//        //    {
//        //        Debug.Log("Root part already selected.");
//        //        return;
//        //    }
//        //    else if (selectedPartName.IndexOf("FacePart") == -1)
//        //    {
//        //        Debug.Log("Selected part is not part of a mesh.");
//        //    }
//        //    else
//        //    {
//        //        meshName = selectedPartName.Substring(0, selectedPartName.IndexOf("FacePart"));
//        //        IPartScript centerPart = GameObject.Find(meshName + "CenterPart").GetComponent<IPartScript>();
//        //        Game.Instance.Designer.SelectPart(centerPart, null, false);
//        //    }
//        //}

//        if (newPart != null)
//        {
//            Debug.Log("Part's parent is: " + newPart.GameObject.transform.parent.name);
//        }
//    }
//}

//    public static class UtilityMethods
//{
//    public static PartModifierData GetModifierData(this IPartScript partScript, string modifier)
//    {
//        return partScript.Data.GetModifierById(modifier);
//    }

//    public static void SafeAddAttribute(this XElement xElement, string name, string value)
//    {
//        if (xElement.Attribute(name) != null)
//        {
//            xElement.Attribute(name).SetValue(value);
//        }
//        else
//        {
//            xElement.Add(new XAttribute(name, value));
//        }
//    }

//    public static XElement GetXElement(this XElement xElement, string name)
//    {
//        return xElement.Descendants().First(x => x.Name == name);
//    }

//    public static Vector3 VectorRotate(this Vector3 v, Vector3 k, double theta)
//    {
//        return v * (float)Cos(theta) + Vector3.Cross(k, v) * (float)Sin(theta) + k * Vector3.Dot(k, v) * (float)(1 - Cos(theta));
//    }
//}

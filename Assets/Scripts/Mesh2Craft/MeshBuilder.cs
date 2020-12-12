using ObjParser;
using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static System.Numerics.Vector3;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Math;
using System.Windows.Markup;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Assets.Scripts;
using Assets.Scripts.Design;

class MeshBuilder
{
    public static XDocument craftDoc;
    public static Obj obj = new Obj();

    public static void BuildMesh(MeshOptions options)
    {
        Game.Instance.Designer.ShowMessage("Importing Model...");

        string objName = Path.GetFileNameWithoutExtension(options.objFile);

        craftDoc = XDocument.Load(options.craftFile);
        XElement partsElement = craftDoc.Descendants().First(x => x.Name == "Parts");
        XElement connectionsElement = craftDoc.Descendants().First(x => x.Name == "Connections");
        XElement bodiesElement = craftDoc.Descendants().First(x => x.Name == "Bodies");
        int lastPartId = Int32.Parse(partsElement.Elements().Last().Attribute("id").Value) + 1; //+1 to account for the root part
        int meshBodyId = Int32.Parse(bodiesElement.Elements().Last().Attribute("id").Value) + 1;

        try
        {
            obj.LoadObj(options.objFile);
        }
        catch (Exception e)
        {
            Debug.LogError("Invalid .obj file: " + options.objFile);
            throw new Exception(e.Message);
        }

        var verts = obj.VertexList;
        var faces = obj.FaceList;

        verts = ScaleObj(verts, options.scale);

        partsElement.Add(new XElement("Part",
            new XAttribute("id", lastPartId),
            new XAttribute("partType", "Fuselage1"),
            new XAttribute("position", "0,0,0"),
            new XAttribute("rotation", "0,0,0"),
            new XAttribute("name", objName + "_root"),
            new XAttribute("commandPodId", "0"),
            new XAttribute("materials", "0,0,0,0,4"),
            new XElement("Drag",
                new XAttribute("drag", "0,0,0,0,0,0"),
                new XAttribute("area", "0,0,0,0,0,0")),
            new XElement("Config"),
            new XElement("Fuselage",
                new XAttribute("bottomScale", "0.1,0.1"),
                new XAttribute("offset", "0,0.1,0"),
                new XAttribute("topScale", "0.1,0.1")),
            new XElement("FuelTank")));

        //adding Body element here, face parts will be added to this instead of making their own
        bodiesElement.Add(new XElement("Body",
                new XAttribute("id", meshBodyId),
                new XAttribute("partIds", lastPartId),
                new XAttribute("mass", "1"),
                new XAttribute("position", "0,0,0"),
                new XAttribute("rotation", "0,0,0"),
                new XAttribute("centerOfMass", "0,0,0")));
        var bodyPartIds = bodiesElement.Elements().First(x => x.Attribute("id").Value == meshBodyId.ToString()).Attribute("partIds");

        int partIdOffset = 0;
        foreach (var face in faces)
        {
            partIdOffset++;

            var vert0 = GetFaceVertPos(face, verts, 0);
            var vert1 = GetFaceVertPos(face, verts, 1);
            var vert2 = GetFaceVertPos(face, verts, 2);

            //get values for shaping part to face
            var basePos = (vert0 + vert1) / 2;
            var partPos = (basePos + vert2) / 2; //not used for structural panels, their position is defined from the base
            var offset = vert2 - basePos;
            var baseWidth = (vert0 - vert1).Length();

            //get orientation and axes
            var vForward = Normalize((vert1 - vert0)); //local Z axis
            var vRight = Normalize(Cross(vForward, offset)); //local X axis
            var vUp = Normalize(Cross(vRight, vForward)); //local Y axis

            //offset to account for the width of the structural panel
            float zFightOffset = (float)(1.0 / 5000);
            basePos += Multiply((float)((options.shellWidth * .05 - zFightOffset)), vRight); 
            // *.05 is bc panels are .1 wide, and being offset by half

            //first rotation of offset vector
            var firstRotAxis = Normalize(Cross(vForward, UnitZ));
            var firstRotAngle = Acos(Dot(vForward, UnitZ));
            var offsetFirstRot = VectorRotate(offset, firstRotAxis, firstRotAngle);
            var vUpRotated = VectorRotate(vUp, firstRotAxis, firstRotAngle);
            if (double.IsNaN(offsetFirstRot.X))
            {
                //handles cases where face is aligned with axis
                Console.WriteLine("First rot axis = NaN");
                if (firstRotAngle > PI / 2)
                {
                    Console.WriteLine("Flipped Z axis");
                    offsetFirstRot = new Vector3(offset.X, offset.Y, offset.Z * -1);
                    vUpRotated = new Vector3(vUp.X, vUp.Y, vUp.Z * -1); //I don't think this is necessary. Z component should be 0 already
                    vForward.Z *= -1;
                }
                else
                {
                    offsetFirstRot = offset;
                    vUpRotated = vUp;
                }
            }

            //second rotation of offset vector
            Vector3 finalRotAxis = Normalize(Cross(vUpRotated, UnitY));
            var finalRotAngle = Acos(Dot(vUpRotated, UnitY));
            var offsetFinalRot = VectorRotate(offsetFirstRot, finalRotAxis, finalRotAngle);
            if (double.IsNaN(offsetFinalRot.X))
            {
                if (finalRotAngle > PI / 2)
                {
                    offsetFinalRot = new Vector3(offsetFirstRot.X, offsetFirstRot.Y * -1, offsetFirstRot.Z);
                }
                else
                {
                    offsetFinalRot = offsetFirstRot;
                }
            }

            offsetFinalRot *= (float).5; //because game values are half of the vector
            baseWidth *= (float).5;

            // using unity's quaternion to get euler angles from forward and up vectors
            Quaternion quat = new Quaternion();
            quat.SetLookRotation(NumericsToUnity(vForward), NumericsToUnity(vUp));
            Vector3 eulerRot = new Vector3(quat.eulerAngles.x, quat.eulerAngles.y, quat.eulerAngles.z);

            //math done, adding to XML

            partsElement.Add(new XElement("Part",
                new XAttribute("id", lastPartId + partIdOffset),
                new XAttribute("partType", "StructuralPanel1"),
                new XAttribute("position", basePos.X + "," + basePos.Y + "," + basePos.Z),
                new XAttribute("rotation", eulerRot.X + "," + eulerRot.Y + "," + eulerRot.Z),
                new XAttribute("name", objName + "_" + partIdOffset),
                new XAttribute("commandPodId", "0"),
                new XAttribute("materials", "0,1,2,3,4"),
                new XElement("Drag",
                    new XAttribute("drag", "0,0,0,0,0,0"),
                    new XAttribute("area", "0,0,0,0,0,0")),
                new XElement("Config",
                    new XAttribute("partScale", options.shellWidth + "," + options.shellWidth + "," + options.shellWidth),
                    new XAttribute("massScale", options.shellWidth * 0.1 * (options.noMass ? 0 : 1))),
                new XElement("Wing",
                    new XAttribute("hingeDistanceFromTrailingEdge", "0.5"),
                    new XAttribute("rootLeadingOffset", baseWidth * (1 / options.shellWidth)),
                    new XAttribute("rootTrailingOffset", baseWidth * (1 / options.shellWidth)),
                    new XAttribute("tipLeadingOffset", "0"),
                    new XAttribute("tipPosition", (2 * offsetFinalRot.X * (1 / options.shellWidth)) + "," + (2 * offsetFinalRot.Y * (1 / options.shellWidth)) + "," + (2 * offsetFinalRot.Z * (1 / options.shellWidth))),
                    new XAttribute("tipTrailingOffset", "0"))));

            connectionsElement.Add(new XElement("Connection",
                new XAttribute("partA", lastPartId + partIdOffset),
                new XAttribute("partB", lastPartId),
                new XAttribute("attachPointsA", "1"),
                new XAttribute("attachPointsB", "0")));

            bodyPartIds.Value += "," + (lastPartId + partIdOffset).ToString();
        }

        string outPath = options.craftFile;
        string modExtension = "_" + Path.GetFileNameWithoutExtension(options.objFile) + ".xml";
        if (!outPath.Contains(modExtension))
        {
            outPath = Path.ChangeExtension(outPath, null);
            outPath += modExtension;
        }
        string newCraftName = Path.GetFileNameWithoutExtension(outPath);
        var craftElement = craftDoc.Element("Craft");
        craftElement.Attribute("name").Value = newCraftName;

        craftDoc.Save(outPath);

        var loader = (Game.Instance.Designer as DesignerScript).CraftLoader;
        loader.LoadCraftInteractive(newCraftName, true, true, null, null, null);

        Game.Instance.Designer.ShowMessage(Path.GetFileName(options.objFile) + "Imported Successfully");

        obj = new Obj();
    }

    public static List<Vertex> ScaleObj(List<Vertex> oldVerts, double scale)
    {
        var newVerts = new List<Vertex>();
        foreach (var vert in oldVerts)
        {
            vert.X *= scale;
            vert.Y *= scale;
            vert.Z *= scale;
            newVerts.Add(vert);
        }
        return newVerts;
    }

    public static Vector3 GetFaceVertPos(Face face, List<Vertex> verts, int i)
    {
        //potentially add support for different axis orientations
        //currently, +y is up, -z is forward (blender default behavior)

        return new Vector3((float)verts[face.VertexIndexList[i] - 1].X, (float)verts[face.VertexIndexList[i] - 1].Y, (float)(verts[face.VertexIndexList[i] - 1].Z * -1));
    }

    public static Vector3 VectorRotate(Vector3 v, Vector3 k, double theta)
    {
        return v * (float)Cos(theta) + Cross(k, v) * (float)Sin(theta) + k * Dot(k, v) * (float)(1 - Cos(theta));
    }

    public static UnityEngine.Vector3 NumericsToUnity(Vector3 v)
    {
        return new UnityEngine.Vector3(v.X, v.Y, v.Z);
    }

    public static string IdFromFile(string craftFile)
    {
        //not necessary. craftId is just the file name

        Debug.Log("starting to look for craftId");
        Debug.Log("Craft file: " + craftFile);

        List<string> ids = Game.Instance.CraftDesigns.GetCraftDesignIds(true);
        string craftId = "";
        bool success = false;

        foreach ( string id in ids)
        {
            string idFile = Game.Instance.CraftDesigns.GetCraftFile(id).FullName;
            idFile = idFile.Replace('\\', '/');
            Debug.Log(idFile);
            if (idFile.Equals(craftFile))
            {
                craftId = id;
                success = true;
                break;
            }
        }

        if (!success)
        {
            Debug.LogError("Couldn't find craftId for file: " + craftFile);
        }

        Debug.Log("Found the craftId?");

        return craftId;
    }
}

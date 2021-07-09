using ObjParser;
using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static System.Math;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Assets.Scripts;
using Assets.Scripts.Design;

static class MeshBuilder
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
        int lastPartId = Int32.Parse(partsElement.Elements().Last().Attribute("id").Value) + 1;
        int partIdOffset = 0;
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

        if (Abs(options.scale - 1) > .0001)
            verts = ScaleObj(verts, options.scale);

        // root part
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
            new XElement("Config",
                new XAttribute("includeInDrag", options.hasDrag ? "true" : "false"),
                new XAttribute("partCollisionHandling", options.hasCollision ? "Default" : "Never"),
                new XAttribute("massScale", options.hasMass ? "1" : "0"),
                new XAttribute("heatShield", options.heatShield),
                new XAttribute("maxTemperature", options.maxTemp)),
            new XElement("Fuselage",
                new XAttribute("autoResize", "false"),
                new XAttribute("bottomScale", "0.05,0.05"),
                new XAttribute("offset", "0,0.05,0"),
                new XAttribute("topScale", "0.05,0.05"))));
        partIdOffset++;

        // handle part
        partsElement.Add(new XElement("Part",
            new XAttribute("id", lastPartId + partIdOffset),
            new XAttribute("partType", "Fuselage1"),
            new XAttribute("position", "0,0,0"),
            new XAttribute("rotation", "0,0,0"),
            new XAttribute("name", objName + "_handle"),
            new XAttribute("commandPodId", "0"),
            new XAttribute("materials", "0,0,0,0,4"),
            new XElement("Drag",
                new XAttribute("drag", "0,0,0,0,0,0"),
                new XAttribute("area", "0,0,0,0,0,0")),
            new XElement("Config",
                new XAttribute("includeInDrag", options.hasDrag ? "true" : "false"),
                new XAttribute("partCollisionHandling", options.hasCollision ? "Default" : "Never"),
                new XAttribute("massScale", options.hasMass ? "1" : "0"),
                new XAttribute("heatShield", options.heatShield),
                new XAttribute("maxTemperature", options.maxTemp)),
            new XElement("Fuselage",
                new XAttribute("autoResize", "false"),
                new XAttribute("bottomScale", "0.1,0.1"),
                new XAttribute("offset", "0,0.1,0"),
                new XAttribute("topScale", "0.1,0.1"))));

        connectionsElement.Add(new XElement("Connection",
            new XAttribute("partA", lastPartId + partIdOffset),
            new XAttribute("partB", lastPartId),
            new XAttribute("attachPointsA", "1"),
            new XAttribute("attachPointsB", "1")));

        //adding Body element here, face parts will be added to this instead of making their own
        bodiesElement.Add(new XElement("Body",
            new XAttribute("id", meshBodyId),
            new XAttribute("partIds", lastPartId),
            new XAttribute("mass", "1"),
            new XAttribute("position", "0,0,0"),
            new XAttribute("rotation", "0,0,0"),
            new XAttribute("centerOfMass", "0,0,0")));
        var bodyPartIds = bodiesElement.Elements().First(x => x.Attribute("id").Value == meshBodyId.ToString())
            .Attribute("partIds");

        Vector3 UnitX = new Vector3(1, 0, 0);
        Vector3 UnitY = new Vector3(0, 1, 0);
        Vector3 UnitZ = new Vector3(0, 0, 1);

        if (!options.fuelTank) // build using structural panels
        {
            options.shellWidth *= 10; // because structural panels are 0.1 wide (1/10th of scale)

            foreach (var face in faces)
            {
                partIdOffset++;

                var vert0 = GetFaceVertPos(face, verts, 0);
                var vert1 = GetFaceVertPos(face, verts, 1);
                var vert2 = GetFaceVertPos(face, verts, 2);

                //get values for shaping part to face
                var basePos = (vert0 + vert1) / 2;
                var partPos =
                    (basePos + vert2) / 2; //not used for structural panels, their position is defined from the base
                var offset = vert2 - basePos;
                var baseWidth = (vert0 - vert1).magnitude;

                //get orientation and axes
                var vForward = (vert1 - vert0).normalized; //local Z axis
                var vRight = Vector3.Cross(vForward, offset).normalized; //local X axis
                var vUp = Vector3.Cross(vRight, vForward).normalized; //local Y axis

                //offset to account for the width of the structural panel
                float zFightOffset = (float) (1.0 / 5000);
                basePos += (float) (options.shellWidth * .05 - zFightOffset) * vRight;
                // *.05 is bc panels are .1 wide, and being offset by half

                Vector3 finalOffset = new Vector3();
                finalOffset.x = ScalarProjection(offset, vRight);
                finalOffset.y = ScalarProjection(offset, vUp);
                finalOffset.z = ScalarProjection(offset, vForward);

                finalOffset *= (float) .5; //because game values are half of the vector
                baseWidth *= (float) .5;

                // using unity's quaternion to get euler angles from forward and up vectors
                Quaternion quat = new Quaternion();
                quat.SetLookRotation((vForward), (vUp));
                Vector3 eulerRot = new Vector3(quat.eulerAngles.x, quat.eulerAngles.y, quat.eulerAngles.z);

                //math done, adding to XML

                partsElement.Add(new XElement("Part",
                    new XAttribute("id", lastPartId + partIdOffset),
                    new XAttribute("partType", "StructuralPanel1"),
                    new XAttribute("position", basePos.x + "," + basePos.y + "," + basePos.z),
                    new XAttribute("rotation", eulerRot.x + "," + eulerRot.y + "," + eulerRot.z),
                    new XAttribute("name", objName + "_" + partIdOffset),
                    new XAttribute("commandPodId", "0"),
                    new XAttribute("materials", "0,1,2,3,4"),
                    new XElement("Drag",
                        new XAttribute("drag", "0,0,0,0,0,0"),
                        new XAttribute("area", "0,0,0,0,0,0")),
                    new XElement("Config",
                        new XAttribute("partScale",
                            options.shellWidth + "," + options.shellWidth + "," + options.shellWidth),
                        new XAttribute("massScale", options.shellWidth * (options.hasMass ? 1 : 0)),
                        new XAttribute("dragScale", options.shellWidth * (options.hasDrag ? 1 : 0)),
                        new XAttribute("includeInDrag", options.hasDrag ? "true" : "false"),
                        new XAttribute("partCollisionHandling", options.hasCollision ? "Default" : "Never"),
                        new XAttribute("heatShield", options.heatShield),
                        new XAttribute("maxTemperature", options.maxTemp)),
                    new XElement("Wing",
                        new XAttribute("hingeDistanceFromTrailingEdge", "0.5"),
                        new XAttribute("rootLeadingOffset", baseWidth * (1 / options.shellWidth)),
                        new XAttribute("rootTrailingOffset", baseWidth * (1 / options.shellWidth)),
                        new XAttribute("tipLeadingOffset", "0"),
                        new XAttribute("tipPosition", (2 * finalOffset.x * (1 / options.shellWidth)) + ","
                            + (2 * finalOffset.y * (1 / options.shellWidth)) + ","
                            + (2 * finalOffset.z * (1 / options.shellWidth))),
                        new XAttribute("tipTrailingOffset", "0"))));

                connectionsElement.Add(new XElement("Connection",
                    new XAttribute("partA", lastPartId + partIdOffset),
                    new XAttribute("partB", lastPartId),
                    new XAttribute("attachPointsA", "1"),
                    new XAttribute("attachPointsB", "0")));

                bodyPartIds.Value += "," + (lastPartId + partIdOffset);
            }

            craftDoc.Save(options.craftFile);

            var loader = (Game.Instance.Designer as DesignerScript).CraftLoader;
            loader.LoadCraftInteractive(Path.GetFileNameWithoutExtension(options.craftFile), true, true,
                "'" + Path.GetFileName(options.objFile) + "'" + " Imported Successfully", null, null);

            // Game.Instance.Designer.ShowMessage("'" + Path.GetFileName(options.objFile) + "'" + " Imported Successfully");

            obj = new Obj();
        }
        else // build using fuel tanks
        {
            options.shellWidth *= 0.5;
            foreach (var face in faces)
            {
                partIdOffset++;

                var vert0 = GetFaceVertPos(face, verts, 0);
                var vert1 = GetFaceVertPos(face, verts, 1);
                var vert2 = GetFaceVertPos(face, verts, 2);

                //get values for shaping part to face
                var basePos = (vert0 + vert1) / 2;
                var partPos = (basePos + vert2) / 2;
                var offset = vert2 - basePos;
                var baseWidth = (vert0 - vert1).magnitude;

                //get orientation and axes
                var vForward = (vert1 - vert0).normalized; //local Z axis
                var vRight = Vector3.Cross(vForward, offset).normalized; //local X axis
                var vUp = Vector3.Cross(vRight, vForward).normalized; //local Y axis

                //offset to account for the width of the part
                float zFightOffset = (float)(1.0 / 5000);
                partPos += (float)(options.shellWidth - zFightOffset) * vRight;

                Vector3 finalOffset = new Vector3();
                finalOffset.x = ScalarProjection(offset, vRight);
                finalOffset.y = ScalarProjection(offset, vUp);
                finalOffset.z = ScalarProjection(offset, vForward);

                // fuselages with 0 length break things
                if (finalOffset.y < 0.0001)
                {
                    //Debug.Log("0 length fuselage skipped: " + partIdOffset + "\n" +
                    //          "offset: " + offsetFinalRot);
                    partIdOffset--;
                    continue;
                }

                // broken offset breaks things
                if (Double.IsInfinity(finalOffset.x) || Double.IsNaN(finalOffset.x) ||
                    Double.IsInfinity(finalOffset.y) || Double.IsNaN(finalOffset.y) ||
                    Double.IsInfinity(finalOffset.z) || Double.IsNaN(finalOffset.z))
                {
                    partIdOffset--;
                    continue;
                }

                finalOffset *= (float).5; //because game values are half of the vector
                baseWidth *= (float).5;

                // using unity's quaternion to get euler angles from forward and up vectors
                Quaternion quat = new Quaternion();
                quat.SetLookRotation((vForward), (vUp));
                Vector3 eulerRot = new Vector3(quat.eulerAngles.x, quat.eulerAngles.y, quat.eulerAngles.z);

                //math done, adding to XML

                partsElement.Add(new XElement("Part",
                    new XAttribute("id", lastPartId + partIdOffset),
                    new XAttribute("partType", "Fuselage1"),
                    new XAttribute("position", partPos.x + "," + partPos.y + "," + partPos.z),
                    new XAttribute("rotation", eulerRot.x + "," + eulerRot.y + "," + eulerRot.z),
                    new XAttribute("name", objName + "_" + partIdOffset),
                    new XAttribute("commandPodId", "0"),
                    new XAttribute("materials", "0,1,2,3,4"),
                    new XAttribute("texture", "Default"),
                    new XElement("Drag",
                        new XAttribute("drag", "0,0,0,0,0,0"),
                        new XAttribute("area", "0,0,0,0,0,0")),
                    new XElement("AttachPoints", 
                        new XElement("AttachPoint",
                            new XAttribute("id", "0"),
                            new XAttribute("enabled", "false")),
                        new XElement("AttachPoint",
                            new XAttribute("id", "2"),
                            new XAttribute("enabled", "false")),
                        new XElement("AttachPoint",
                            new XAttribute("id", "3"),
                            new XAttribute("enabled", "false")),
                        new XElement("AttachPoint",
                            new XAttribute("id", "4"),
                            new XAttribute("enabled", "false")),
                        new XElement("AttachPoint",
                            new XAttribute("id", "5"),
                            new XAttribute("enabled", "false"))),
                    new XElement("Config",
                        new XAttribute("includeInDrag", options.hasDrag ? "true" : "false"),
                        new XAttribute("partCollisionHandling", options.hasCollision ? "Default" : "Never"),
                        new XAttribute("massScale", options.hasMass ? "1" : "0"),
                        new XAttribute("heatShield", options.heatShield), 
                        new XAttribute("maxTemperature", options.maxTemp)),
                    new XElement("Fuselage",
                        new XAttribute("autoResize", "false"),
                        new XAttribute("bottomScale", options.shellWidth + "," + baseWidth),
                        new XAttribute("cornerRadiuses", "0,0,0,0,0,0,0,0"),
                        new XAttribute("offset", finalOffset.x + "," + finalOffset.y + "," + finalOffset.z),
                        new XAttribute("topScale", options.shellWidth + ",0"))
                     ));

                connectionsElement.Add(new XElement("Connection",
                    new XAttribute("partA", lastPartId + partIdOffset),
                    new XAttribute("partB", lastPartId),
                    new XAttribute("attachPointsA", "1"),
                    new XAttribute("attachPointsB", "1")));

                bodyPartIds.Value += "," + (lastPartId + partIdOffset);
            }

            craftDoc.Save(options.craftFile);

            Game.Instance.Designer.ShowMessage("Loading...");
            var loader = (Game.Instance.Designer as DesignerScript).CraftLoader;
            loader.LoadCraftInteractive(Path.GetFileNameWithoutExtension(options.craftFile), true, true,
                "'" + Path.GetFileName(options.objFile) + "'" + " Imported Successfully", null, null);

            obj = new Obj();
            craftDoc = null;
        }
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

        return new Vector3((float)verts[face.VertexIndexList[i] - 1].X, 
            (float)verts[face.VertexIndexList[i] - 1].Y, 
            (float)(verts[face.VertexIndexList[i] - 1].Z * -1));
    }

    public static Vector3 VectorRotate(Vector3 v, Vector3 k, double theta)
    {
        return v * (float)Cos(theta) + Vector3.Cross(k, v) * (float)Sin(theta) + k * Vector3.Dot(k, v) * (float)(1 - Cos(theta));
    }

    public static float ScalarProjection(Vector3 a, Vector3 b)
    {
        return (float) Vector3.Dot(a, b) / b.magnitude;
    }
}

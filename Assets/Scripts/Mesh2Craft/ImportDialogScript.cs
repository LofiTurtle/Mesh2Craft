using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModApi.Ui;
using UnityEditor;
using Assets.Scripts;
using System;
using Assets.Scripts.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UI.Xml;
using TMPro;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using ObjParser;

public class ImportDialogScript : MonoBehaviour
{
    private static string objPath = Mod.Instance.objDirectory;
    private List<string> objList = Directory.EnumerateFiles(objPath, "*.obj").ToList();
    //private List<string> objList = new List<string>();
    private static XmlElement activeObjElement;
    private static List<string> activeClasses = new List<string>();
    private static List<string> inactiveClasses = new List<string>();
    private static Obj _obj = new Obj();
    private static string _objName = "";
    private static double _scale = 1;
    private static double _shellWidth = 0.001;
    private static bool _noMass = false;
    private static bool _newCraft = true;

    private IXmlLayoutController _controller;
    private XmlLayout _xmlLayout;

    public void OnLayoutRebuilt(IXmlLayoutController controller)
    {
        _controller = controller;
        _xmlLayout = (XmlLayout)controller.XmlLayout;
        Debug.Log("Updating obj list");
        UpdateObjList(objList);
    }

    public void Close()
    {
        ResetOptionsValues();
        this._controller.XmlLayout.Hide(() => GameObject.Destroy(this.gameObject), true);
    }

    private void ResetOptionsValues()
    {
        _scale = 1;
        _shellWidth = 0.001;
        _noMass = false;
        _objName = "";
        _newCraft = true;
    }

    private void UpdateObjList(List<string> list)
    {
        XmlElement vLayout = _xmlLayout.GetElementById("obj-list");
        XmlElement listItemTemplate = _xmlLayout.GetElementById("obj-template");
        List<string> objNames = new List<string>();

        foreach (string obj in list)
        {
            string objName = Path.GetFileName(obj);
            objNames.Add(objName);

            if (_xmlLayout.GetElementById("obj-" + objName) == null)
            {

                XmlElement listItem = GameObject.Instantiate(listItemTemplate);
                XmlElement component = listItem.GetComponent<XmlElement>();

                component.Initialise(_xmlLayout, (RectTransform)listItem.transform, listItemTemplate.tagHandler);
                vLayout.AddChildElement(component);

                component.SetAndApplyAttribute("active", "true");
                component.SetAndApplyAttribute("id", objName);
                component.GetElementByInternalId<TextMeshProUGUI>("obj-name").text = objName;
                //component.SetAndApplyAttribute("text", objName);
                component.ApplyAttributes();
            }
        }

        List<Button> buttons = vLayout.GetComponentsInChildren<Button>().ToList();
        List<XmlElement> xmlElements = vLayout.GetComponentsInChildren<XmlElement>().ToList();

        for (int i = 0; i < buttons.Count; i++)
        {
            if (!objNames.Contains(buttons[i].GetComponentInChildren<TextMeshProUGUI>().text))
                vLayout.RemoveChildElement(xmlElements[i]);
        }
    }

    public void OnObjButtonCLicked(XmlElement objElement)
    {
        //Debug.Log("ObjButton clicked");

        if (activeClasses.Any())
        {
            activeClasses.Add("btn");
            activeClasses.Add("button-primary");
        }

        if (inactiveClasses.Any())
        {
            inactiveClasses.Add("btn");
            inactiveClasses.Add("button-menu");
        }

        //Debug.Log("Setting classes");
        if (activeObjElement != null)
        {
            activeObjElement.SetClass("btn");
            activeObjElement.SetAndApplyAttribute("textColors", "ButtonText|ButtonText|ButtonText|ButtonText");
        }

        activeObjElement = objElement;
        activeObjElement.SetClass("btn", "btn-primary");
        activeObjElement.SetAndApplyAttribute("textColors", "White|White|White|White");

        UpdateObjDetails();
    }

    private void UpdateObjDetails()
    {
        XmlElement modelName = _xmlLayout.GetElementById("model-name");
        string objName = activeObjElement.id;

        modelName.SetText(objName);

        XmlElement isTris = _xmlLayout.GetElementById("is-tris");
        if (!ObjIsTris())
        {
            isTris.SetText("Model is not triangulated");
            isTris.SetAndApplyAttribute("color", "White");
        }
        else
        {

            isTris.SetText("Model is triangulated");
            isTris.SetAndApplyAttribute("color", "LabelText");
        }

        XmlElement faceCount = _xmlLayout.GetElementById("face-count");
        faceCount.SetText(GetObj().FaceList.Count + " Parts");
    }

    private static bool ObjIsTris()
    {
        foreach (var face in GetObj().FaceList)
        {
            if (face.VertexIndexList.Length != 3)
                return false;
        }

        return true;
    }

    public void OnImportButtonClicked()
    {
        // Debug.Log("ImportButton clicked");

        if (!ObjIsTris())
        {
            _xmlLayout.GetElementById("not-tris-warning").SetAndApplyAttribute("active", "true");
            _xmlLayout.GetElementById("hide-main-panel").SetAndApplyAttribute("active", "true");
            return;
        }

        if (activeObjElement != null) // make sure an obj is selected
        {
            string craftFile;

            string oldCraftId = (Game.Instance.Designer as DesignerScript).CraftScript.Data.Name;
            string newCraftId;
            if (!oldCraftId.Equals("New")) // if craft is saved
            {

                if (_newCraft) // make new file, or overwrite current one
                {
                    newCraftId = oldCraftId + "_" + Path.GetFileNameWithoutExtension(_objName);
                    (Game.Instance.Designer as DesignerScript).CraftScript.Data.Name = newCraftId;
                }
                else
                {
                    newCraftId = oldCraftId;
                }
                Game.Instance.Designer.SaveCraft(newCraftId, newCraftId, false);
                craftFile = Game.Instance.CraftDesigns.RootFolderPath + newCraftId + ".xml";
            }
            else // if craft is not saved. Basically always make new craft
            {
                newCraftId = oldCraftId + "_" + Path.GetFileNameWithoutExtension(_objName);
                (Game.Instance.Designer as DesignerScript).CraftScript.Data.Name = newCraftId;

                Game.Instance.Designer.SaveCraft(newCraftId, newCraftId, false);
                craftFile = Game.Instance.CraftDesigns.RootFolderPath + newCraftId + ".xml";
            }

            string objFile = objPath + activeObjElement.id;

            var options = new MeshOptions(craftFile, objFile, _shellWidth, _scale, _noMass);

            Debug.Log("Calling BuildMesh() with the following options: ");
            Debug.Log(options);
            MeshBuilder.BuildMesh(options);

            Close();
        }
        else
        {
            Game.Instance.Designer.ShowMessage("No file selected. Select an .obj file and try again");
        }
    }

    public void OnTestSaveClicked()
    {
        string newId = "AABC";
        (Game.Instance.Designer as DesignerScript).CraftScript.Data.Name = newId;
        Debug.Log("Saving craft with id " + newId);
        Game.Instance.Designer.SaveCraft(newId, newId, false);
    }

    public void OnScaleChanged(string value)
    {
        _scale = double.Parse(value);
    }

    public void OnNoMassChanged(string value)
    {
        _noMass = Boolean.Parse(value);
    }

    public void OnNewCraftChanged(string value)
    {
        _newCraft = Boolean.Parse(value);
    }

    public void OnShellWidthChanged(string value)
    {
        _shellWidth = Double.Parse(value);
    }

    public void NotTrisWarningClose()
    {
        _xmlLayout.GetElementById("not-tris-warning").SetAndApplyAttribute("active", "false");
        _xmlLayout.GetElementById("hide-main-panel").SetAndApplyAttribute("active", "false");
    }

    private static Obj GetObj()
    {
        if (_objName.Equals(activeObjElement.id))
            return _obj;
        else if (activeObjElement != null)
        {
            _obj = new Obj();
            _obj.LoadObj(Mod.Instance.objDirectory + activeObjElement.id);
            _objName = activeObjElement.id;
            return _obj;
        }
        else
        {
            Debug.LogError("GetObj() returned null");
            return _obj;
        }
    }

    public static void PrintList(List<string> list)
    {
        Debug.Log("Printing objList with length " + list.Count);
        StringBuilder listedItems = new StringBuilder();
        foreach (string item in list)
        {
            if (listedItems.Length != 0)
            {
                listedItems.Append("\n");
            }
            listedItems.Append(item);
        }
        Debug.Log(listedItems);
    }
}

public class MeshOptions
{
    public string objFile, craftFile;
    public double shellWidth, scale;
    public bool noMass;

    public MeshOptions(string craftFile, string objFile, double shellWidth, double scale, bool noMass)
    {
        this.craftFile = craftFile;
        this.objFile = objFile;
        this.shellWidth = Math.Max(shellWidth * 10, .001);
        this.scale = scale;
        this.noMass = noMass;
    }

    public override string ToString()
    {
        return "obj File: " + objFile + "\n"
               + "Craft File: " + craftFile + "\n";
    }
}

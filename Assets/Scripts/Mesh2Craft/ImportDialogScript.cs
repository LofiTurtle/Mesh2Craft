using System.Collections.Generic;
using UnityEngine;
using ModApi.Ui;
using Assets.Scripts;
using System;
using Assets.Scripts.Design;
using System.IO;
using System.Linq;
using System.Text;
using UI.Xml;
using TMPro;
using UnityEngine.UI;
using ObjParser;

public class ImportDialogScript : MonoBehaviour
{
    private static readonly string objDir = Mod.Instance.objDirectory;
    private readonly List<string> objList = Directory.EnumerateFiles(objDir, "*.obj").ToList();
    //private List<string> objList = new List<string>();
    private static XmlElement _activeObjElement;
    private static Obj _obj = new Obj();
    private static string _objName = "";
    private static double _scale = 1;
    private static double _shellWidth = 0.001;
    private static bool _hasMass = true;
    private static bool _hasDrag = true;
    private static bool _hasCollision = true;
    private static bool _fuelTank = true;
    private static bool _newCraft = true;

    private IXmlLayoutController _controller;
    private XmlLayout _xmlLayout;

    public void OnLayoutRebuilt(IXmlLayoutController controller)
    {
        _controller = controller;
        _xmlLayout = (XmlLayout)controller.XmlLayout;
        //Debug.Log("Updating obj list");
        UpdateObjList(objList);
    }

    public void Close()
    {
        ResetOptionsValues();
        this._controller.XmlLayout.Hide(() => GameObject.Destroy(this.gameObject), true);
    }

    private void ResetOptionsValues()
    {
        _objName = "";
        _scale = 1;
        _shellWidth = 0.001;
        _hasMass = true;
        _hasDrag = true;
        _hasCollision = true;
        _fuelTank = true;
        _newCraft = true;
    }

    private void UpdateObjList(List<string> list)
    {
        XmlElement vLayout = _xmlLayout.GetElementById("obj-list");
        XmlElement listItemTemplate = _xmlLayout.GetElementById("obj-template");
        List<string> objNames = new List<string>();

        if (list.Any())
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
                    component.ApplyAttributes();
                }
            }
        else
        {
            XmlElement message = _xmlLayout.GetElementById("no-obj-message");
            message.SetAndApplyAttribute("text", "No .obj files found. Paste .obj files into " + objDir);
            message.SetAndApplyAttribute("active", "true");
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
        //Debug.Log("Setting classes");
        if (_activeObjElement != null)
        {
            _activeObjElement.SetClass("btn");
            _activeObjElement.SetAndApplyAttribute("textColors", "ButtonText|ButtonText|ButtonText|ButtonText");
        }

        _activeObjElement = objElement;
        _activeObjElement.SetClass("btn", "btn-primary");
        _activeObjElement.SetAndApplyAttribute("textColors", "White|White|White|White");

        UpdateObjDetails();
    }

    private void UpdateObjDetails()
    {
        XmlElement modelName = _xmlLayout.GetElementById("model-name");
        string objName = _activeObjElement.id;

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
            ShowNotTrisWarning();
            return;
        }

        if (_activeObjElement != null) // make sure an obj is selected
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

            string objFile = objDir + _activeObjElement.id;

            var options = new MeshOptions(craftFile, objFile, _shellWidth, _scale, _hasMass, _hasDrag, _hasCollision, _fuelTank);

            Debug.Log("Calling BuildMesh() with the following options:\n" + options);
            //Debug.Log(options);
            MeshBuilder.BuildMesh(options);

            Close();
        }
        else
        {
            Game.Instance.Designer.ShowMessage("No file selected. Select an .obj file and try again");
        }
    }

    private void HideMainPanel(string active)
    {

        _xmlLayout.GetElementById("hide-main-panel").SetAndApplyAttribute("active", active);
    }

    public void ShowNotTrisWarning()
    {
        _xmlLayout.GetElementById("not-tris-warning").SetAndApplyAttribute("active", "true");
        HideMainPanel("true");
    }

    public void NotTrisWarningClose()
    {
        _xmlLayout.GetElementById("not-tris-warning").SetAndApplyAttribute("active", "false");
        HideMainPanel("false");
    }

    public void OnAdvancedButtonClicked()
    {
        _xmlLayout.GetElementById("advanced-options-panel").SetAndApplyAttribute("active", "true");
        HideMainPanel("true");
    }

    public void CloseAdvancedPanel()
    {
        _xmlLayout.GetElementById("advanced-options-panel").SetAndApplyAttribute("active", "false");
        HideMainPanel("false");
    }

    private static Obj GetObj()
    {
        if (_objName.Equals(_activeObjElement.id))
            return _obj;
        else if (_activeObjElement != null)
        {
            _obj = new Obj();
            _obj.LoadObj(Mod.Instance.objDirectory + _activeObjElement.id);
            _objName = _activeObjElement.id;
            return _obj;
        }
        else
        {
            Debug.LogError("GetObj() returned null");
            return _obj;
        }
    }

    public void OnScaleChanged(string value)
    {
        if (value.Length > 0)
            _scale = double.Parse(value);
    }

    public void OnHasMassChanged(string value)
    {
        _hasMass = Boolean.Parse(value);
    }

    public void OnNewCraftChanged(string value)
    {
        _newCraft = Boolean.Parse(value);
    }

    public void OnShellWidthChanged(string value)
    {
        if (value.Length > 0)
            _shellWidth = Double.Parse(value);
    }

    public void OnHasDragChanged(string value)
    {
        _hasDrag = Boolean.Parse(value);
    }

    public void OnHasCollisionChanged(string value)
    {
        _hasCollision = Boolean.Parse(value);
    }

    public void OnFuelTankChanged(string value)
    {
        _fuelTank = Boolean.Parse(value);
    }
}

public class MeshOptions
{
    public string objFile, craftFile;
    public double shellWidth, scale;
    public bool hasMass, hasDrag, hasCollision, fuelTank;

    public MeshOptions(string craftFile, string objFile, double shellWidth, double scale,
        bool hasMass, bool hasDrag, bool hasCollision, bool fuelTank)
    {
        this.craftFile = craftFile;
        this.objFile = objFile;
        this.shellWidth = Math.Max(shellWidth, .001);
        this.scale = scale;
        this.hasMass = hasMass;
        this.hasDrag = hasDrag;
        this.hasCollision = hasCollision;
        this.fuelTank = fuelTank;
    }

    public override string ToString()
    {
        return "obj File: " + objFile + "\n"
               + "Craft File: " + craftFile + "\n"
               + "Shell Width: " + shellWidth + "\n"
               + "Scale: " + scale + "\n"
               + "No Mass: " + hasMass + "\n"
               + "No Drag: " + hasDrag + "\n"
               + "No Collision: " + hasCollision + "\n"
               + "Fuel Tank: " + fuelTank;
    }
}

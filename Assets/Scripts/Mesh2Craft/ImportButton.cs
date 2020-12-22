using Assets.Scripts;
using ModApi.Ui;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Ui;
using UnityEngine;

public class ImportButton : MonoBehaviour
{
    private static string _m2cButtonId = "mesh-2-craft-button";

    private IXmlLayoutController _controller;
    private static ImportDialogScript _dialogScript;
    public FlyoutScript _Flyout = new FlyoutScript();

    public static void Initialize()
    {
        var userInterface = Game.Instance.UserInterface;
        userInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Design.DesignerUi, OnBuildDesignerUi);
    }

    public void OnLayoutRebuilt(IXmlLayoutController controller)
    {
        this._controller = controller;
        //_Flyout.Initialize(((XmlLayout)_controller.XmlLayout).GetElementById("flyout-Mesh2Craft"));
        //_Flyout.Open();
    }

    public static void OnM2cButtonClicked()
    {
        if (_dialogScript != null)
        {
            _dialogScript.Close();
            _dialogScript = null;
        }
        else
        {
            var ui = Game.Instance.UserInterface;
            _dialogScript = ui.BuildUserInterfaceFromResource<ImportDialogScript>("Mesh2Craft/Designer/MtoCDialog", (script, controller) => script.OnLayoutRebuilt(controller));
        }
    }

    private static void OnBuildDesignerUi(BuildUserInterfaceXmlRequest request)
    {
        var ns = XmlLayoutConstants.XmlNamespace;
        var viewButton = request.XmlDocument
            .Descendants(ns + "Panel")
            .First(x => (string)x.Attribute("internalId") == "flyout-view");

        viewButton.Parent.Add(
            new XElement(
                ns + "Panel",
                new XAttribute("id", _m2cButtonId),
                new XAttribute("class", "toggle-button audio-btn-click"),
                new XAttribute("name", "ButtonPanel.TutorialDesignerButton"),
                new XAttribute("tooltip", "Mesh2Craft"),
                new XElement(
                    ns + "Image",
                    new XAttribute("class", "toggle-button-icon"),
                    new XAttribute("sprite", "Mesh2Craft/Sprites/m2cIcon_thicker"))));

        request.AddOnLayoutRebuiltAction(XmlLayoutController =>
        {
            var button = XmlLayoutController.XmlLayout.GetElementById(_m2cButtonId);
            button.AddOnClickEvent(OnM2cButtonClicked);
        });
    }


}

using UnityEngine;
using TMPro;

public class MerchantDialogueUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject merchantRoot;

    [Header("Panels")]
    public GameObject dialoguePanel;
    public GameObject vendorSellPanel;
    public GameObject vendorBuyPanel;
    public GameObject questPanel;

    [Header("Player Inventory UI")]
    public GameObject menuRoot;
    public GameObject inventoryPanel;

    [Header("Text")]
    public TMP_Text merchantNameText;
    public TMP_Text dialogueText;

    private string currentMerchantName = "Merchant";

    void Awake()
    {
        if (merchantRoot == null)
            merchantRoot = gameObject;

        CloseAll();
    }

    void Update()
    {
        if (merchantRoot == null || !merchantRoot.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (dialoguePanel != null && dialoguePanel.activeSelf)
                CloseAll();
            else
                BackToDialogue();
        }
    }

    public void OpenDialogue(string merchantName)
    {
        currentMerchantName = merchantName;

        if (merchantRoot != null)
            merchantRoot.SetActive(true);

        ClosePanelsOnly();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (merchantNameText != null)
            merchantNameText.text = currentMerchantName;

        if (dialogueText != null)
            dialogueText.text = "What can I do for you?";
    }

    public void BackToDialogue()
    {
        if (merchantRoot != null)
            merchantRoot.SetActive(true);

        ClosePanelsOnly();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (merchantNameText != null)
            merchantNameText.text = currentMerchantName;

        if (dialogueText != null)
            dialogueText.text = "What can I do for you?";
    }

    public void OpenBuy()
    {
        OpenInventory();
        OpenPanel(vendorBuyPanel);
    }

    public void OpenSell()
    {
        OpenInventory();
        OpenPanel(vendorSellPanel);
    }

    public void OpenQuests()
    {
        OpenPanel(questPanel);
    }

    public void HealPlayer()
    {
        PlayerStats player = PlayerStats.LocalInstance;
        if (player != null)
            player.ServerVendorHeal();
    }

    void OpenInventory()
    {
        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }

    void OpenPanel(GameObject panel)
    {
        if (merchantRoot != null)
            merchantRoot.SetActive(true);

        ClosePanelsOnly();

        if (panel != null)
            panel.SetActive(true);
    }

    public void CloseAll()
    {
        ClosePanelsOnly();

        if (merchantRoot != null)
            merchantRoot.SetActive(false);
    }

    void ClosePanelsOnly()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (vendorSellPanel != null) vendorSellPanel.SetActive(false);
        if (vendorBuyPanel != null) vendorBuyPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    public Transform recipeListParent; // Content objesi
    public GameObject recipePrefab;
    public List<CraftRecipe> recipes;

    public bool isOpenCrafting = false;
    public GameObject freelookcamera;
    public Animator craftingAnimator;

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        foreach (Transform child in recipeListParent)
            Destroy(child.gameObject);

        foreach (var recipe in recipes)
        {
            var recipeGO = Instantiate(recipePrefab, recipeListParent);
            recipeGO.transform.Find("Icon").GetComponent<Image>().sprite = recipe.resultItem.icon;
            recipeGO.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = recipe.recipeName;

            // Gereksinim listesi
            string reqText = "";
            foreach (var req in recipe.requirements)
            {
                reqText += $"{req.item.itemName} x{req.amount}\n";
            }
            recipeGO.transform.Find("RequirementsText").GetComponent<TextMeshProUGUI>().text = reqText;

            // Craft butonu
            var craftButton = recipeGO.transform.Find("CraftButton").GetComponent<Button>();
            craftButton.onClick.AddListener(() =>
            {
                if (CraftManager.Instance.Craft(recipe))
                {
                    RefreshUI();
                }
            });

            // Malzeme yoksa buton kapalý
            craftButton.interactable = CraftManager.Instance.CanCraft(recipe);
        }
    }
    public void LockCamrea()
    {
        if (isOpenCrafting)
        {
            isOpenCrafting = false;
            freelookcamera.SetActive(true);
            PlayerManager.Instance.OnEnable();
            return;
        }
        else
        {
            isOpenCrafting = true;
            freelookcamera.SetActive(false);
            PlayerManager.Instance.OnDisable();

        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            craftingAnimator.SetTrigger("Open");
            LockCamrea();
            RefreshUI();
        }
    }

}

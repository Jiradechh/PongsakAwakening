using UnityEngine;

public class ShopGems : MonoBehaviour
{
    public void BuyItem(int price)
    {
        if (GameManager.Instance.SpendGems(price))
        {
            Debug.Log("Item purchased with Gems!");
        }
        else
        {
            Debug.Log("Not enough Gems!");
        }
    }
}

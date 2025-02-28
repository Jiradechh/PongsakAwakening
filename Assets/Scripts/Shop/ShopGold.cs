using UnityEngine;

public class ShopGold : MonoBehaviour
{
    public void BuyItem(int price)
    {
        if (GameManager.Instance.SpendGold(price))
        {
            Debug.Log("Item purchased with Gold!");
        }
        else
        {
            Debug.Log("Not enough Gold!");
        }
    }
}

using UnityEngine;
    public class Enemy: BaseEntity
    {
        public Element element;
        
        public void TakeDamage(int damage, Element element)
        {
            if (this.element != element)
            {
                return;
            }
            
            this.Health -= damage;
            Debug.Log("Take damage: " + damage);

            if (Health <= 0)
            {
                Debug.Log($"{name} has been defeated!");
                Destroy(this.gameObject, 1f);
            }
        }
    }

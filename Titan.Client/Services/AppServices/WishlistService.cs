using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using Titan.Client.Services.Apibase;
using Titan.Client.Services.Models;

namespace Titan.Client.Services.AppServices
{
    public class WishlistService : ApiBase
    {
        public event Action? Changed;
        public List<ProductDto> Items { get; private set; } = new();
        public int Count => Items.Count;

        public WishlistService(HttpClient h, ILocalStorageService s, NavigationManager n) : base(h, s, n) { }

        public async Task LoadAsync()
        {
            var result = await GetAsync<List<ProductDto>>("api/wishlist");
            Items = result?.Data ?? new();
            Changed?.Invoke();
        }

        public bool Has(Guid id) => Items.Any(p => p.Id == id);

        public async Task<bool> ToggleAsync(Guid productId)
        {
            var wasIn = Has(productId);
            var r = wasIn
                ? await DelAsync<bool>($"api/wishlist/{productId}")
                : await PostAsync<bool>($"api/wishlist/{productId}");
            
            if (r?.Success == true) await LoadAsync();
            return !wasIn;
        }
    }
}

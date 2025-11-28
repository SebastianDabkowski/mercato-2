using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed cart repository.
/// </summary>
public sealed class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cart is not null)
        {
            await LoadItemsAsync(cart, cancellationToken);
        }

        return cart;
    }

    public async Task<Cart?> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);

        if (cart is not null)
        {
            await LoadItemsAsync(cart, cancellationToken);
        }

        return cart;
    }

    public async Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, cancellationToken);

        if (cart is not null)
        {
            await LoadItemsAsync(cart, cancellationToken);
        }

        return cart;
    }

    public async Task AddAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    public void Update(Cart cart)
    {
        // Handle cart items explicitly since they're in a separate table
        var existingItemIds = _context.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .Select(ci => ci.Id)
            .ToHashSet();

        var currentItemIds = cart.Items.Select(i => i.Id).ToHashSet();

        // Remove items that are no longer in the cart
        var itemsToRemove = _context.CartItems
            .Where(ci => ci.CartId == cart.Id && !currentItemIds.Contains(ci.Id));
        _context.CartItems.RemoveRange(itemsToRemove);

        // Add new items and update existing ones
        foreach (var item in cart.Items)
        {
            if (existingItemIds.Contains(item.Id))
            {
                _context.CartItems.Update(item);
            }
            else
            {
                _context.CartItems.Add(item);
            }
        }

        _context.Carts.Update(cart);
    }

    public void Delete(Cart cart)
    {
        // Remove all items first
        var items = _context.CartItems.Where(ci => ci.CartId == cart.Id);
        _context.CartItems.RemoveRange(items);
        _context.Carts.Remove(cart);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    private async Task LoadItemsAsync(Cart cart, CancellationToken cancellationToken)
    {
        var items = await _context.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync(cancellationToken);

        cart.LoadItems(items);
    }
}

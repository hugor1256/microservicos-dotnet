using RestauranteService.Dtos;

namespace RestauranteService
{
    public interface IItemServiceHttpClient
    {
        public Task EnviarRestaurantesParaItemService(RestauranteReadDto readDto);
    }
}


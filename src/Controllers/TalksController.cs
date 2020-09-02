using System;
using System.Threading.Tasks;
using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CoreCodeCamp.Controllers
{
    [ApiController]
    [Route("api/camps/{moniker}/talks")]
    public class TalksController : ControllerBase
    {
        private readonly ICampRepository _repository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public TalksController(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _repository = repository;
            _mapper = mapper;
            _linkGenerator = linkGenerator;
        }

        [HttpGet]
        public async Task<ActionResult<TalkModel[]>> Get(string moniker)
        {
            try
            {
                var talks = await _repository.GetTalksByMonikerAsync(moniker, true);
                if (talks == null || talks.Length <= 0)
                    return NotFound("Talks not found");
                return _mapper.Map<TalkModel[]>(talks);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Failed to get Talks for the Camps with Moniker {moniker}");
            }
        }


        [HttpGet("{id:int}")]
        public async Task<ActionResult<TalkModel>> Get(string moniker, int id)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker,id, true);
                if (talk == null) return NotFound("Talk not found");
                return _mapper.Map<TalkModel>(talk);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Failed to get Talk for the Camps with Moniker {moniker}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TalkModel>> Post(string moniker, TalkModel talkModel)
        {
            try
            {
                var camp = await _repository.GetCampAsync(moniker);

                if (camp == null) return BadRequest($"Camp not found for the Moniker ${moniker}");

                var talk = _mapper.Map<Talk>(talkModel);

                talk.Camp = camp;

                if (talkModel.Speaker == null) return BadRequest("Speaker ID is required");

                var speaker = await _repository.GetSpeakerAsync(talkModel.Speaker.SpeakerId);
                if(speaker == null) return BadRequest("Speaker not found");

                talk.Speaker = speaker;

                _repository.Add(talk);

                if (await _repository.SaveChangesAsync())
                {
                    var url = _linkGenerator.GetPathByAction(HttpContext,
                        "Get",
                        values: new {moniker, id = talk.TalkId});

                    return Created(url,_mapper.Map<TalkModel>(talk));
                }
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Failed to create Talk for the Camps with Moniker {moniker}");
            }

            return BadRequest("Failed to save talk");
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TalkModel>> Put(string moniker, int id, TalkModel talkModel )
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker,id,true);

                if (talk == null) return NotFound("Couldn't find the talk");

                _mapper.Map(talkModel, talk);

                if (talkModel.Speaker != null)
                {
                    var speaker = await _repository.GetSpeakerAsync(talkModel.Speaker.SpeakerId);
                    if(speaker == null) return BadRequest("Speaker not found");

                    talk.Speaker = speaker;
                }

                if (await _repository.SaveChangesAsync())
                {
                    return _mapper.Map<TalkModel>(talk);
                }
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Failed to update Talk with Moniker {moniker}");
            }
            return BadRequest("Failed to update database with talk");
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(string moniker,int id)
        {
            try
            {
                var talk = await _repository.GetTalkByMonikerAsync(moniker, id);
                if (talk == null) return NotFound("Talk not found");

                _repository.Delete(talk);

                if (await _repository.SaveChangesAsync())
                {
                    return Ok();
                }
                else
                {
                    return BadRequest("Failed to Delete Talk");
                }
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Failed to Delete Talk with Moniker {moniker}");
            }
        }
    }
}
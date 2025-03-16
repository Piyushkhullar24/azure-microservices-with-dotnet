using Microsoft.AspNetCore.Mvc;
using Wpm.Clinic.Application;
using Wpm.Clinic.DataAccess;
using Wpm.Clinic.SAGA;

namespace Wpm.Clinic.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsulationController(ClinicApplicationService clinicApplicationService, ClinicDBContext clinicDBContext, ConsulationSagaOrchestrator consulationSagaOrchestrator) : ControllerBase
{

    [HttpPost("/start")]
    public async Task<IActionResult> Start(StartConsulationCommand command)
    {
        //var result = await clinicApplicationService.Handle(command);
        //return Ok(result);

        await consulationSagaOrchestrator.StartSaga(command.PatientId);
        return Ok("Saga Started");
    }

    [HttpGet]
    public async Task<IActionResult> Get(int patientId)
    {
        //var result = await clinicApplicationService.Handle(command);
        //return Ok(result);

        var consultaion = await clinicApplicationService.GetConsulation(patientId);
        if(consultaion == null)
        {

            return NotFound();
        }
        return Ok(consultaion);
    }


    public record StartConsulationCommand(int PatientId);
}

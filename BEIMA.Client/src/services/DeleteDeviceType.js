import axios from 'axios';
const API_URL = process.env.REACT_APP_API_URL;

/**
 * POSTs an Devite Type ID for the API to delete
 *
 * @param The ID of the device type to delete
 * @return Error message or a succes indicator
 */
export default async function deleteDevice(deviceTypeId) {
  //performs the post and returns an error message or a succes indicator
  const dbCall = await axios.post(API_URL + "device-type/" + deviceTypeId + "/delete").catch(function (error) {
      if (error.response) {
        return error.response;
    }
  });

  const response = {
    status: dbCall.status,
    response: dbCall.data
  }

  return response;
}

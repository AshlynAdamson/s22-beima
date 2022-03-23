import axios from 'axios';
const API_URL = process.env.REACT_APP_API_URL;

/**
 * GET API call to get a list of  buildings in the database
 *
 * @return JSON with list of all buildings or error message on failure
 */
const GetBuildingList = async() => {
  const buildingListCall = await axios.get(API_URL + "building-list").catch(function (error) {
      if (error.response) {
        return error.response;
    }
  });

  let filteredFields = buildingListCall.data.map((item) => {
    return {
    id: item._id,
    name: item.name,
    number: item.number,
    notes: item.notes,
    latitude: item.location.latitude,
    longitude: item.location.longitude
    }
  })

  const response = {
    status: buildingListCall.status,
    response: filteredFields
  }

  return response;
}

export default GetBuildingList